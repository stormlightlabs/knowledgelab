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
	Snippet    string    `json:"snippet"` // Matched text snippet with context
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

	tags := make([]string, len(note.Tags))
	for i, tag := range note.Tags {
		tags[i] = tag.Name
	}

	content := s.buildSearchableContent(note)

	doc := SearchDocument{
		NoteID:     note.ID,
		Title:      note.Title,
		Path:       note.Path,
		Content:    content,
		Tags:       tags,
		ModifiedAt: note.ModifiedAt,
	}

	s.removeNoteFromIndex(note.ID)

	docIdx := len(s.docs)
	s.docs = append(s.docs, doc)

	for _, tag := range tags {
		s.tagIndex[tag] = append(s.tagIndex[tag], docIdx)
	}

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
				Snippet:    "",
			})
		}
	} else {
		queryTokens := s.tokenize(query.Query)

		for _, idx := range candidates {
			doc := s.docs[idx]

			bm25Score := s.index.Score(idx, query.Query)
			fuzzyBonus := s.calculateFuzzyBonus(doc, queryTokens)
			exactBonus := s.calculateExactMatchBonus(doc, query.Query)
			totalScore := bm25Score + (exactBonus * 2.0) + (fuzzyBonus * 0.5)

			if totalScore > 0 {
				snippet := s.extractSnippet(doc.Content, queryTokens)
				results = append(results, SearchResult{
					NoteID:     doc.NoteID,
					Title:      doc.Title,
					Path:       doc.Path,
					Score:      totalScore,
					Tags:       doc.Tags,
					ModifiedAt: doc.ModifiedAt,
					Snippet:    snippet,
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

		content := s.buildSearchableContent(&note)

		doc := SearchDocument{
			NoteID:     note.ID,
			Title:      note.Title,
			Path:       note.Path,
			Content:    content,
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

// buildSearchableContent combines note content with frontmatter fields for indexing.
// Includes title, content, aliases, type, and frontmatter values.
func (s *SearchService) buildSearchableContent(note *domain.Note) string {
	var parts []string

	parts = append(parts, note.Title)
	parts = append(parts, note.Content)
	parts = append(parts, note.Aliases...)

	if note.Type != "" {
		parts = append(parts, note.Type)
	}

	for key, value := range note.Frontmatter {
		if str, ok := value.(string); ok {
			parts = append(parts, key, str)
		} else if arr, ok := value.([]any); ok {
			for _, item := range arr {
				if str, ok := item.(string); ok {
					parts = append(parts, str)
				}
			}
		}
	}

	return strings.Join(parts, " ")
}

// calculateExactMatchBonus returns a score boost for exact phrase matches.
// Exact matches in title get highest boost, content matches get moderate boost.
func (s *SearchService) calculateExactMatchBonus(doc SearchDocument, query string) float64 {
	queryLower := strings.ToLower(query)
	titleLower := strings.ToLower(doc.Title)
	contentLower := strings.ToLower(doc.Content)

	score := 0.0

	if strings.Contains(titleLower, queryLower) {
		score += 10.0
	}

	if strings.Contains(contentLower, queryLower) {
		score += 5.0
	}
	return score
}

// calculateFuzzyBonus returns a score boost for fuzzy matches using edit distance.
// Rewards terms that are similar to query terms even if not exact matches.
func (s *SearchService) calculateFuzzyBonus(doc SearchDocument, queryTokens []string) float64 {
	if len(queryTokens) == 0 {
		return 0.0
	}

	contentTokens := s.tokenize(doc.Content)
	titleTokens := s.tokenize(doc.Title)

	totalBonus := 0.0

	for _, queryToken := range queryTokens {
		for _, titleToken := range titleTokens {
			distance := levenshteinDistance(queryToken, titleToken)
			maxLen := max(len(queryToken), len(titleToken))

			if distance <= 2 && float64(maxLen-distance)/float64(maxLen) > 0.6 {
				totalBonus += 2.0 * (1.0 - float64(distance)/float64(maxLen))
			}
		}

		for _, contentToken := range contentTokens {
			distance := levenshteinDistance(queryToken, contentToken)
			maxLen := max(len(queryToken), len(contentToken))

			if distance <= 2 && float64(maxLen-distance)/float64(maxLen) > 0.6 {
				totalBonus += 0.5 * (1.0 - float64(distance)/float64(maxLen))
			}
		}
	}

	return totalBonus
}

// extractSnippet extracts a text snippet showing query matches with context.
// Matched terms are wrapped with [[ ]] markers for frontend highlighting.
func (s *SearchService) extractSnippet(content string, queryTokens []string) string {
	if len(queryTokens) == 0 || content == "" {
		return ""
	}

	contentLower := strings.ToLower(content)
	snippetMaxLen := 150

	bestPos := -1
	bestToken := ""

	for _, token := range queryTokens {
		pos := strings.Index(contentLower, token)
		if pos != -1 && (bestPos == -1 || pos < bestPos) {
			bestPos = pos
			bestToken = token
		}
	}

	if bestPos == -1 {
		if len(content) > snippetMaxLen {
			return content[:snippetMaxLen] + "..."
		}
		return content
	}

	contextBefore := 40
	contextAfter := snippetMaxLen - contextBefore - len(bestToken)

	start := max(bestPos-contextBefore, 0)

	end := bestPos + len(bestToken) + contextAfter
	if end > len(content) {
		end = len(content)
	}

	if start > 0 {
		for start < len(content) && content[start] != ' ' && start < bestPos {
			start++
		}
		if start < len(content) && content[start] == ' ' {
			start++
		}
	}

	if end < len(content) {
		for end > bestPos && content[end] != ' ' && content[end] != '.' && content[end] != '!' && content[end] != '?' {
			end--
		}
	}

	snippet := strings.TrimSpace(content[start:end])

	snippet = s.highlightMatches(snippet, queryTokens)

	if start > 0 {
		snippet = "..." + snippet
	}
	if end < len(content) {
		snippet = snippet + "..."
	}

	return snippet
}

// highlightMatches wraps matched query tokens with [[ ]] markers for frontend highlighting.
func (s *SearchService) highlightMatches(snippet string, queryTokens []string) string {
	type match struct {
		start int
		end   int
	}

	snippetLower := strings.ToLower(snippet)
	matches := []match{}

	for _, token := range queryTokens {
		tokenLower := strings.ToLower(token)
		searchPos := 0

		for {
			pos := strings.Index(snippetLower[searchPos:], tokenLower)
			if pos == -1 {
				break
			}

			actualPos := searchPos + pos
			matches = append(matches, match{start: actualPos, end: actualPos + len(token)})
			searchPos = actualPos + len(token)
		}
	}

	if len(matches) == 0 {
		return snippet
	}

	sortedMatches := make([]match, len(matches))
	copy(sortedMatches, matches)

	for i := 0; i < len(sortedMatches)-1; i++ {
		for j := i + 1; j < len(sortedMatches); j++ {
			if sortedMatches[j].start < sortedMatches[i].start {
				sortedMatches[i], sortedMatches[j] = sortedMatches[j], sortedMatches[i]
			}
		}
	}

	mergedMatches := []match{}
	if len(sortedMatches) > 0 {
		current := sortedMatches[0]
		for i := 1; i < len(sortedMatches); i++ {
			if sortedMatches[i].start <= current.end {
				if sortedMatches[i].end > current.end {
					current.end = sortedMatches[i].end
				}
			} else {
				mergedMatches = append(mergedMatches, current)
				current = sortedMatches[i]
			}
		}
		mergedMatches = append(mergedMatches, current)
	}

	result := strings.Builder{}
	lastEnd := 0

	for _, m := range mergedMatches {
		result.WriteString(snippet[lastEnd:m.start])
		result.WriteString("[[")
		result.WriteString(snippet[m.start:m.end])
		result.WriteString("]]")
		lastEnd = m.end
	}

	result.WriteString(snippet[lastEnd:])

	return result.String()
}

// levenshteinDistance calculates the edit distance between two strings.
// Used for fuzzy matching to find similar but not identical terms.
func levenshteinDistance(s1, s2 string) int {
	if len(s1) == 0 {
		return len(s2)
	}
	if len(s2) == 0 {
		return len(s1)
	}

	matrix := make([][]int, len(s1)+1)
	for i := range matrix {
		matrix[i] = make([]int, len(s2)+1)
		matrix[i][0] = i
	}
	for j := range matrix[0] {
		matrix[0][j] = j
	}

	for i := 1; i <= len(s1); i++ {
		for j := 1; j <= len(s2); j++ {
			cost := 1
			if s1[i-1] == s2[j-1] {
				cost = 0
			}

			// Here we're getting the smallest of deletion, insertion, substitution
			matrix[i][j] = min(min(
				matrix[i-1][j]+1,
				matrix[i][j-1]+1),
				matrix[i-1][j-1]+cost)
		}
	}

	return matrix[len(s1)][len(s2)]
}
