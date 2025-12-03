# Theming

The application uses the Base16 color system for theming, providing consistent color semantics across light and dark variants.
All UI components reference base16 color variables, making theme switching seamless.

## Base16 Color Roles

Base16 defines 16 colors with specific semantic roles. The application applies these colors consistently throughout the interface.

### Background and Foreground Colors (base00-base07)

**base00** - Default Background
Primary background color for the application canvas, editor area, and main content regions.

**base01** - Lighter Background
Used for status bars, sidebars, panels, and code blocks. Provides subtle differentiation from the main background.

**base02** - Selection Background
Background color for selected text, active items, and highlighted regions. Also used for borders and dividers.

**base03** - Comments and Secondary Text
Color for comments, secondary text, placeholders, and disabled states. Lower contrast than primary text.

**base04** - Dark Foreground
Used in status bars and secondary UI elements where a muted foreground is needed.

**base05** - Default Foreground
Primary text color for body text, paragraphs, and most UI content. High contrast against base00.

**base06** - Light Foreground
Used for headings, emphasized text, and UI elements that need slightly more prominence than base05.

**base07** - Lightest Foreground
Reserved for the highest contrast text, typically unused in dark themes or used sparingly for maximum emphasis.

### Accent Colors (base08-base0F)

**base08** - Red
Used for errors, warnings, deletions, and dangerous actions. Also represents variables in syntax highlighting.

**base09** - Orange
Used for numbers, integers, and numeric literals in code. Also used for secondary warnings.

**base0A** - Yellow
Used for search highlights, classes, types, and important notices. High visibility color.

**base0B** - Green
Used for strings, success states, additions, and positive feedback. Represents growth and confirmation.

**base0C** - Cyan
Used for regular expressions, special strings, and escape sequences. Also used for links and navigation.

**base0D** - Blue
Used for functions, methods, primary actions, and interactive elements. Most common accent color.

**base0E** - Magenta
Used for keywords, control flow, and special operators in code. Also used for tags.

**base0F** - Brown
Used for deprecated features, legacy code, and special cases. Less commonly used.

## Theme File Format

Themes are defined in YAML files following the base16 specification. Each theme file contains metadata and a 16-color palette.

### Required Fields

```yaml
system: "base16"
name: "Theme Name"
author: "Author Name"
slug: "theme-slug"
variant: "dark"  # or "light"
palette:
  base00: "#RRGGBB"
  base01: "#RRGGBB"
  base02: "#RRGGBB"
  base03: "#RRGGBB"
  base04: "#RRGGBB"
  base05: "#RRGGBB"
  base06: "#RRGGBB"
  base07: "#RRGGBB"
  base08: "#RRGGBB"
  base09: "#RRGGBB"
  base0A: "#RRGGBB"
  base0B: "#RRGGBB"
  base0C: "#RRGGBB"
  base0D: "#RRGGBB"
  base0E: "#RRGGBB"
  base0F: "#RRGGBB"
```

### Schema

- **system**: Must be "base16" to indicate base16 theme format
- **name**: Human-readable theme name displayed in the UI
- **author**: Theme author name or attribution
- **slug**: URL-friendly identifier used internally (lowercase, hyphens)
- **variant**: Either "dark" or "light" to indicate theme brightness
- **palette**: Object containing all 16 base colors in hex format

All color values must be 6-digit hex codes (e.g., "#161821"). The hash prefix is required.

## Theme File Locations

### Bundled Themes

The application includes curated themes, available immediately without additional installation.

### User Theme Directory

Custom themes can be placed in the user configuration directory (future feature):

- **macOS/Linux**: `~/.config/knowledgelab/themes/`
- **Windows**: `%APPDATA%\knowledgelab\themes\`

User themes in this directory will be loaded alongside bundled themes.

## Creating Custom Themes

### Starting from an Existing Theme

The easiest way to create a custom theme is to copy an existing theme file and modify it:

1. Copy the above below example into a file with your theme name (e.g., `my-theme.yaml`)
2. Update the metadata fields (name, author, slug)
3. Adjust the palette colors to your preference
4. Save the file to the user theme directory

### Example: Custom Dark Theme

Here's a complete example of a custom dark theme:

```yaml
system: "base16"
name: "Midnight Purple"
author: "Your Name"
slug: "midnight-purple"
variant: "dark"
palette:
  base00: "#1a1625"  # Deep purple background
  base01: "#2d2438"  # Slightly lighter background
  base02: "#443750"  # Selection and borders
  base03: "#6e6582"  # Comments
  base04: "#8f8ba3"  # Muted foreground
  base05: "#e0d9f0"  # Main text
  base06: "#f2eef8"  # Emphasized text
  base07: "#ffffff"  # Maximum contrast
  base08: "#ff6b9d"  # Pink for errors/variables
  base09: "#ffb454"  # Orange for numbers
  base0A: "#ffd966"  # Yellow for classes
  base0B: "#8cd991"  # Green for strings
  base0C: "#7dd3c0"  # Cyan for regex
  base0D: "#7d9cff"  # Blue for functions
  base0E: "#c792ea"  # Purple for keywords
  base0F: "#ab7967"  # Brown for deprecated
```

### Example: Custom Light Theme

Light themes require careful contrast selection for readability:

```yaml
system: "base16"
name: "Paper"
author: "Your Name"
slug: "paper-light"
variant: "light"
palette:
  base00: "#f8f5f0"  # Warm white background
  base01: "#ebe8e3"  # Slightly darker background
  base02: "#dedad5"  # Selection and borders
  base03: "#9e9a95"  # Comments and secondary
  base04: "#6e6a65"  # Muted foreground
  base05: "#2f2a25"  # Main text (dark)
  base06: "#1a1510"  # Emphasized text
  base07: "#000000"  # Maximum contrast
  base08: "#b3312d"  # Red for errors
  base09: "#c86b33"  # Orange for numbers
  base0A: "#9c8e2d"  # Yellow/gold for classes
  base0B: "#3e7f35"  # Green for strings
  base0C: "#2f7e7a"  # Teal for regex
  base0D: "#2951c7"  # Blue for functions
  base0E: "#7c3ea3"  # Purple for keywords
  base0F: "#8b5d3b"  # Brown for deprecated
```

## Design Guidelines

### Contrast Ratios

Ensure sufficient contrast for accessibility:

- **base05 on base00**: Minimum 7:1 for body text (AAA)
- **base03 on base00**: Minimum 4.5:1 for secondary text (AA)
- **Accent colors on base00**: Minimum 4.5:1 for interactive elements

### Color Progression

Background colors should progress from darkest to lightest (dark themes) or lightest to darkest (light themes):

- Dark theme: base00 < base01 < base02 < base03
- Light theme: base00 > base01 > base02 > base03

Foreground colors should provide clear hierarchy:

- base05: Primary text
- base06: Emphasized text
- base04: Muted text
- base03: Secondary/disabled text

### Accent Color Selection

Choose accent colors that:

- Are visually distinct from each other
- Maintain consistent saturation levels
- Work well for syntax highlighting
- Have clear semantic associations (red=error, green=success, etc.)

## Theme Switching

Themes are applied by setting CSS custom properties on the document root:

```javascript
--color-base00: #161821
--color-base01: #1e2132
// ... all 16 colors
```

The application provides duplicate variables for compatibility.
Both reference the same values and can be used interchangeably in CSS.

- `--baseXX`: Standard base16 naming
- `--color-baseXX`: Semantic naming

## Bundled Theme Sources

Bundled themes are sourced from the [tinted-theming](https://github.com/tinted-theming/schemes) project, which maintains a curated collection of high-quality base16 themes. These themes are widely used across editors and terminal applications.
