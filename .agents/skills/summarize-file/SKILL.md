---
name: summarize-file
description: Summarize the contents of a file when the user asks for a summary
triggers:
  - summarize this file
  - give me a summary
---

# Summarize File Skill

## Instructions

When the user asks to summarize a file:

1. Read the file contents carefully
2. Produce:
   - A 1-line summary
   - 3 bullet key points

## Output format

SUMMARY:
<one sentence>

KEY POINTS:
- point 1
- point 2
- point 3

## Notes

- Keep it concise
- Do not add extra commentary