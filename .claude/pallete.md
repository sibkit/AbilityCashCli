# Claude Code palette

Extracted from the Claude Code CLI binary (`claude` 4.18, Bun-compiled PE).
Hex values are literal strings found in the binary; usage is inferred from the
surrounding code.

## Brand

| Color       | Hex       | RGB              | Truecolor ANSI                  | Usage in binary                                        |
|-------------|-----------|------------------|---------------------------------|--------------------------------------------------------|
| Crab orange | `#da7756` | 218, 119, 86     | `\033[38;2;218;119;86m`         | `$8.hex("#da7756")` → `o1H`, heatmap blocks (▒▓█)     |
| Cream       | `#F4F1EA` | 244, 241, 234    | `\033[38;2;244;241;234m`        | Documented as Anthropic brand background               |

## Warm accents (from the web/design palette embedded in the binary)

| Hex       | RGB            | Note                         |
|-----------|----------------|------------------------------|
| `#fbf0df` | 251, 240, 223  | light cream                  |
| `#f6dece` | 246, 222, 206  | peach cream                  |
| `#f3d5a3` | 243, 213, 163  | warm sand                    |
| `#ccbea7` | 204, 190, 167  | warm tan                     |
| `#e39437` | 227, 148, 55   | amber-orange                 |
| `#b45309` | 180, 83, 9     | dark amber                   |
| `#78350f` | 120, 53, 15    | dark brown                   |

## Neutrals (Tailwind slate, also present in the binary)

| Hex       | Token      |
|-----------|------------|
| `#f8fafc` | slate-50   |
| `#f1f5f9` | slate-100  |
| `#e2e8f0` | slate-200  |
| `#cbd5e1` | slate-300  |
| `#94a3b8` | slate-400  |
| `#64748b` | slate-500  |
| `#475569` | slate-600  |
| `#334155` | slate-700  |
| `#0f172a` | slate-900  |

## CLI chalk usage frequency

How often each chalk method is called in the CLI (from static grep of
`<var>.<method>(` patterns). Gives a sense of what the TUI actually renders:

| Method    | Calls | Typical purpose                      |
|-----------|-------|--------------------------------------|
| `dim`     | 174   | secondary/muted text                 |
| `bold`    | 148   | emphasis                             |
| `red`     | 56    | errors                               |
| `yellow`  | 40    | warnings                             |
| `green`   | 18    | success                              |
| `gray`    | 14    | muted captions                       |
| `white`   | 4     | rare                                 |
| `magenta` | 4     | rare                                 |
| `blue`    | 4     | rare                                 |
| `cyan`    | 2     | rare                                 |
| `.hex("#da7756")` | 2 | brand orange accents         |

`dim` and `gray` are the workhorses for subdued content; `.hex("#da7756")`
is reserved for brand accent — use it sparingly.

## Recipe for a status line

```bash
# Brand accent (model name, etc.)
printf "\033[38;2;218;119;86m%s\033[0m" "$text"

# Muted secondary info (context/session/rate-limit)
printf "\033[90m%s\033[0m"               "$text"   # chalk.gray equivalent
printf "\033[2m%s\033[0m"                "$text"   # chalk.dim equivalent

# Path (light neutral)
printf "\033[38;5;248m%s\033[0m"         "$path"

# Warm accent that pairs with crab orange without clashing
printf "\033[38;2;204;190;167m%s\033[0m" "$branch"   # #ccbea7 tan
```

## Notes

- Truecolor (`\033[38;2;R;G;Bm`) requires a 24-bit terminal: Windows Terminal,
  modern VS Code terminal, iTerm2, alacritty, etc. `cmd.exe` legacy console
  will ignore it.
- Only `#da7756` and `#F4F1EA` are explicitly *branded* in the source. All
  other hexes are assets (SVG, docs, Tailwind defaults) that happen to ship
  inside the binary — treat them as inspiration for pairing, not as
  canonical brand tokens.