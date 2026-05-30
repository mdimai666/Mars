# HttpInFormSaveFilesNode

This component is designed to save files from an IFormCollection form (multipart/form-data) to storage.

Used in conjunction with HttpInNode.

## 🚀 Features
- **Template Parsing:** Support for dynamic placeholders for dates, times, unique identifiers, and file metadata.
- **Security (Path Traversal Protection):** Automatic sanitization of filenames and fields from prohibited operating system characters (`/`, `\`, `:`, `*`, etc.).

---

## 📅 Available Placeholders in the Template

### FilePathTemplate

Dynamic Path Generation and Secure Saving

You can combine the following tokens in the `FilePathTemplate` parameter:

| Token | Description | Example Result |
| :--- | :--- | :--- |
| **File Details** | | |
| `{file_name}` | Full filename with extension | `photo.jpg` |
| `{file_name_only}` | Filename without extension | `photo` |
| `{file_ext}` | File extension with a period (always lowercase) | `.jpg` |
| `{field_name}` | HTML form input (property) name | `avatar` |
| `{guid}` | Unique 32-character hash (no hyphens) | `d3b07384d113ede...` |
| `{unique_suffix}` | Unique suffix | `20260529_206c1bc9` |
| `{unique_file_name}` | Unique file name | `photo_20260529_206c1bc9.jpg` |
| **Date** | | |
| `{yyyy}` | Year (4 digits) | `2026` |
| `{yy}` | Year (2 digits) | `26` |
| `{MM}` | Month with leading zero | `05` |
| `{M}` | Month without leading zero | `5` |
| `{DD}` | Day with leading zero | `09` |
| `{D}` | Day without leading zero | `9` |
| **Time** | | |
| `{HH}` | Hour in 24-hour format with zero | `14` |
| `{H}` | Hour in 24-hour format without leading zero | `14` |
| `{mm}` | Minutes with leading zero | `05` |
| `{ss}` | Seconds with leading zero | `34` |

> case sensetive
