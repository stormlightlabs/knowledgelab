package service

import (
	"sort"
	"strings"
	"sync"
	"time"

	"notes/backend/domain"

	"github.com/covrom/bm25s"
)

// SearchService provides full-text search capabilities using BM25 ranking.
type SearchService struct {
	mu sync.RWMutex

	// BM25 index
	index *bm25s.BM25S

	// Document metadata (maps index position to note info)
	docs []SearchDocument

	// Tag index for fast tag filtering
	tagIndex map[string][]int
}

// SearchDocument represents a searchable document with metadata.
type SearchDocument struct {
	NoteID     string
	Title      string
	Path       string
	Content    string
	Tags       []string
	ModifiedAt time.Time
}

// SearchQuery represents a search request with filters.
type SearchQuery struct {
	Query      string     // Search query text
	Tags       []string   // Filter by tags (AND logic)
	PathPrefix string     // Filter by path prefix
	DateFrom   *time.Time `ts_type:"string"`
	DateTo     *time.Time `ts_type:"string"`
	Limit      int        // Maximum number of results (0 = no limit)
}

// SearchResult represents a single search result with ranking score.
type SearchResult struct {
	NoteID     string    `json:"noteId"`
	Title      string    `json:"title"`
	Path       string    `json:"path"`
	Score      float64   `json:"score"`
	Tags       []string  `json:"tags"`
	ModifiedAt time.Time `json:"modifiedAt" ts_type:"string"`
}

// NewSearchService creates a new search service.
func NewSearchService() *SearchService {
	return &SearchService{
		docs:     []SearchDocument{},
		tagIndex: make(map[string][]int),
	}
}

// IndexNote adds or updates a note in the search index.
func (s *SearchService) IndexNote(note *domain.Note) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	// Extract tag names
	tags := make([]string, len(note.Tags))
	for i, tag := range note.Tags {
		tags[i] = tag.Name
	}

	doc := SearchDocument{
		NoteID:     note.ID,
		Title:      note.Title,
		Path:       note.Path,
		Content:    note.Content,
		Tags:       tags,
		ModifiedAt: note.ModifiedAt,
	}

	// Remove existing document if present
	s.removeNoteFromIndex(note.ID)

	// Add document
	docIdx := len(s.docs)
	s.docs = append(s.docs, doc)

	// Update tag index
	for _, tag := range tags {
		s.tagIndex[tag] = append(s.tagIndex[tag], docIdx)
	}

	// Rebuild BM25 index
	s.rebuildBM25Index()

	return nil
}

// RemoveNote removes a note from the search index.
func (s *SearchService) RemoveNote(noteID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.removeNoteFromIndex(noteID)
	s.rebuildBM25Index()
}

// Search performs a full-text search with optional filters.
func (s *SearchService) Search(query SearchQuery) ([]SearchResult, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.index == nil || len(s.docs) == 0 {
		return []SearchResult{}, nil
	}

	// Apply filters to get candidate document indices
	candidates := s.applyCandidateFilters(query)
	if len(candidates) == 0 {
		return []SearchResult{}, nil
	}

	results := []SearchResult{}

	if query.Query == "" {
		for _, idx := range candidates {
			doc := s.docs[idx]
			results = append(results, SearchResult{
				NoteID:     doc.NoteID,
				Title:      doc.Title,
				Path:       doc.Path,
				Score:      0,
				Tags:       doc.Tags,
				ModifiedAt: doc.ModifiedAt,
			})
		}
	} else {
		for _, idx := range candidates {
			doc := s.docs[idx]

			score := s.index.Score(idx, query.Query)

			if score > 0 {
				results = append(results, SearchResult{
					NoteID:     doc.NoteID,
					Title:      doc.Title,
					Path:       doc.Path,
					Score:      score,
					Tags:       doc.Tags,
					ModifiedAt: doc.ModifiedAt,
				})
			}
		}

		sort.Slice(results, func(i, j int) bool {
			return results[i].Score > results[j].Score
		})
	}

	if query.Limit > 0 && len(results) > query.Limit {
		results = results[:query.Limit]
	}

	return results, nil
}

// IndexAll rebuilds the search index from a list of notes.
func (s *SearchService) IndexAll(notes []domain.Note) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.docs = make([]SearchDocument, 0, len(notes))
	s.tagIndex = make(map[string][]int)

	for _, note := range notes {
		tags := make([]string, len(note.Tags))
		for i, tag := range note.Tags {
			tags[i] = tag.Name
		}

		doc := SearchDocument{
			NoteID:     note.ID,
			Title:      note.Title,
			Path:       note.Path,
			Content:    note.Content,
			Tags:       tags,
			ModifiedAt: note.ModifiedAt,
		}

		docIdx := len(s.docs)
		s.docs = append(s.docs, doc)

		for _, tag := range tags {
			s.tagIndex[tag] = append(s.tagIndex[tag], docIdx)
		}
	}

	s.rebuildBM25Index()

	return nil
}

// applyCandidateFilters returns document indices that match filter criteria.
func (s *SearchService) applyCandidateFilters(query SearchQuery) []int {
	candidates := make(map[int]bool)
	for i := range s.docs {
		candidates[i] = true
	}

	if len(query.Tags) > 0 {
		tagMatches := make(map[int]int)
		for _, tag := range query.Tags {
			if indices, ok := s.tagIndex[tag]; ok {
				for _, idx := range indices {
					tagMatches[idx]++
				}
			}
		}

		for idx := range candidates {
			if tagMatches[idx] < len(query.Tags) {
				delete(candidates, idx)
			}
		}
	}

	if query.PathPrefix != "" {
		for idx := range candidates {
			if !strings.HasPrefix(s.docs[idx].Path, query.PathPrefix) {
				delete(candidates, idx)
			}
		}
	}

	if query.DateFrom != nil {
		for idx := range candidates {
			if s.docs[idx].ModifiedAt.Before(*query.DateFrom) {
				delete(candidates, idx)
			}
		}
	}
	if query.DateTo != nil {
		for idx := range candidates {
			if s.docs[idx].ModifiedAt.After(*query.DateTo) {
				delete(candidates, idx)
			}
		}
	}

	result := make([]int, 0, len(candidates))
	for idx := range candidates {
		result = append(result, idx)
	}

	return result
}

// rebuildBM25Index recreates the BM25 index from current documents.
func (s *SearchService) rebuildBM25Index() {
	if len(s.docs) == 0 {
		s.index = nil
		return
	}

	documents := make([]string, len(s.docs))
	for i, doc := range s.docs {
		searchText := doc.Title + " " + doc.Content
		documents[i] = searchText
	}

	s.index = bm25s.New(documents, bm25s.WithTokenizer(func(text string) []string {
		return s.tokenize(text)
	}))
}

// removeNoteFromIndex removes a note from internal structures.
func (s *SearchService) removeNoteFromIndex(noteID string) {
	newDocs := make([]SearchDocument, 0, len(s.docs))
	for _, doc := range s.docs {
		if doc.NoteID != noteID {
			newDocs = append(newDocs, doc)
		}
	}
	s.docs = newDocs

	s.tagIndex = make(map[string][]int)
	for i, doc := range s.docs {
		for _, tag := range doc.Tags {
			s.tagIndex[tag] = append(s.tagIndex[tag], i)
		}
	}
}

// tokenize splits text into searchable tokens.
func (s *SearchService) tokenize(text string) []string {
	text = strings.ToLower(text)
	replacer := strings.NewReplacer(
		".", " ", ",", " ", "!", " ", "?", " ",
		";", " ", ":", " ", "(", " ", ")", " ",
		"[", " ", "]", " ", "{", " ", "}", " ",
		"\"", " ", "'", " ", "\n", " ", "\t", " ",
	)
	text = replacer.Replace(text)

	words := strings.Fields(text)
	tokens := make([]string, 0, len(words))

	for _, word := range words {
		if len(word) > 1 {
			tokens = append(tokens, word)
		}
	}

	return tokens
}
