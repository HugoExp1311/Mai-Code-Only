# Documentation System Guide

## Overview

This project uses the **Claude Code Development Kit (CCDK)** - a 3-tier documentation system designed for effective AI-assisted development at scale.

**📖 For a comprehensive project guide, see [`CCDK.md`](../CCDK.md) in the project root.**

## Documentation Philosophy

The 3-tier system ensures:
- **Simple tasks stay simple** - Basic edits don't require extensive context loading
- **Complex tasks get proper context** - Multi-system changes have full architectural understanding
- **Automatic context delivery** - The right documentation loads at the right time
- **Consistent AI knowledge** - All AI sessions and sub-agents share the same understanding

## The 3-Tier System

### Tier 1: Foundation (Always Loaded)

**Purpose**: Core project understanding that every AI session needs

**Files**:
- `/CLAUDE.md` - Master project context
- `/docs/ai-context/project-structure.md` - Detailed architecture
- `/docs/ai-context/system-integration.md` - How systems interact
- `/docs/ai-context/deployment-infrastructure.md` - Build and deployment
- `/docs/ai-context/docs-overview.md` - Documentation navigation map
- `/docs/ai-context/handoff.md` - Current state and session continuity

**When to Update**: After major architectural changes, new system additions, or significant refactoring

### Tier 2: Component Context

**Purpose**: Deep understanding of specific major systems

**Location**: `CONTEXT.md` files in component directories

**Examples**:
- `Assets/Scripts/CONTEXT.md` - Core scripts and game logic
- `Assets/Live2D/CONTEXT.md` - Character animation system
- `Audio System/CONTEXT.md` - FMOD integration and audio

**Content**:
- Component architecture and responsibilities
- Key classes and their roles
- Public APIs and interfaces
- Integration points with other systems
- Common patterns and conventions

**When to Update**: After significant changes to component architecture or APIs

### Tier 3: Feature Context

**Purpose**: Implementation details for specific features

**Location**: `CONTEXT.md` files in feature subdirectories

**Examples**:
- `Assets/Scripts/DialogueSystem/CONTEXT.md` - Dialogue implementation
- `Assets/Scripts/Characters/CONTEXT.md` - Character controller details
- `Assets/Scripts/SaveSystem/CONTEXT.md` - Save/load implementation

**Content**:
- Implementation patterns and algorithms
- Configuration details
- Edge cases and gotchas
- Code examples
- Performance considerations

**When to Update**: After feature implementation changes or bug fixes

## MCP Integration

This project uses **MCP (Model Context Protocol)** tools to enable efficient, token-optimized development workflows. MCP tools allow AI assistants to access code and documentation on-demand rather than loading everything upfront.

### Available MCP Servers

**Serena MCP** - Symbol-based code operations:
- `mcp_serena_find_file` - Locate files in the project
- `mcp_serena_get_symbols_overview` - Get high-level view of code structure
- `mcp_serena_find_symbol` - Find specific classes, methods, properties
- `mcp_serena_replace_symbol_body` - Edit code at symbol level
- `mcp_serena_search_for_pattern` - Search for code patterns
- `mcp_serena_find_referencing_symbols` - Find where code is used
- `mcp_serena_read_memory` / `mcp_serena_write_memory` - Persist project knowledge

**Context7 MCP** - On-demand documentation:
- `mcp_context7_resolve_library_id` - Find library documentation IDs
- `mcp_context7_get_library_docs` - Fetch Unity, Live2D, FMOD documentation

### MCP-First Development Approach

**MANDATORY**: Always use MCP tools as the primary method for code operations.

**When to Use MCP Tools**:
- ✅ Finding files → `mcp_serena_find_file`
- ✅ Reading code → `mcp_serena_get_symbols_overview` + `mcp_serena_find_symbol`
- ✅ Editing code → `mcp_serena_replace_symbol_body`
- ✅ Searching patterns → `mcp_serena_search_for_pattern`
- ✅ Finding references → `mcp_serena_find_referencing_symbols`
- ✅ Fetching API docs → Context7 MCP tools

**When to Load Tier 2/3 Documentation**:
- Load Tier 2 CONTEXT.md when you need component architecture understanding
- Load Tier 3 CONTEXT.md when you need feature implementation details
- Use `mcp_serena_read_file` to load specific CONTEXT.md files on-demand

See `.kiro/session-guideline.md` for detailed MCP workflow patterns and examples.

## Using the Documentation

### For Developers

**Starting a Task**:
1. Tier 1 docs load automatically (via Session Guideline)
2. Use MCP tools to explore code structure
3. Load Tier 2/3 context only when needed for architectural understanding
4. Use `/docs/ai-context/docs-overview.md` to find relevant CONTEXT.md files

**Example - Working on Dialogue**:
```
Auto-loaded: Session Guideline (Tier 1 essentials)
MCP tools: Find and read DialogueManager symbols
Load if needed: Assets/Scripts/DialogueSystem/CONTEXT.md (Tier 2)
Load if needed: Assets/Localization/CONTEXT.md (Tier 3)
```

### For AI Sessions

**Simple Task** (e.g., "Fix a typo in dialogue UI"):
- Session Guideline auto-loaded
- Use MCP tools to find and edit specific symbol
- No additional context needed

**Medium Task** (e.g., "Add new character expression"):
- Session Guideline auto-loaded
- Use MCP tools to explore relevant code
- Load `Assets/Live2D/CONTEXT.md` (Tier 2) for architecture
- Load `Assets/Scripts/Characters/CONTEXT.md` (Tier 3) for patterns

**Complex Task** (e.g., "Implement save system"):
- Session Guideline auto-loaded
- Use MCP tools to explore multiple components
- Load multiple Tier 2 contexts (Scripts, UI, Data)
- Review `docs/ai-context/system-integration.md`
- Create new `Assets/Scripts/SaveSystem/CONTEXT.md` (Tier 3)

### Progressive Context Loading Strategy

**Level 1: MINIMAL** (Simple tasks - bug fixes, small edits):
- Session Guideline only
- MCP tools for file discovery and symbol reading
- Token cost: ~2000 tokens

**Level 2: STANDARD** (Medium tasks - feature additions, refactoring):
- Session Guideline + relevant Tier 2 component contexts
- MCP tools + Context7 for API documentation
- MCP memory retrieval for known patterns
- Token cost: ~5000-8000 tokens

**Level 3: DEEP** (Complex tasks - architectural changes, multi-system integration):
- Session Guideline + multiple Tier 2 contexts
- Tier 3 feature contexts as needed
- Integration patterns from system-integration.md
- Context7 comprehensive documentation
- MCP memory creation for new discoveries
- Token cost: ~10000-15000 tokens

**Context Escalation**: Start with MINIMAL, escalate to STANDARD if task complexity increases, escalate to DEEP if cross-system work is needed.

## Creating CONTEXT.md Files

### Template for Tier 2 (Component Level)

```markdown
# [Component Name] - Component Context

## Purpose
Brief description of what this component does

## Architecture
High-level structure and design

## Key Classes
- **ClassName**: Responsibility

## Public APIs
Public methods and interfaces

## Integration Points
How this connects to other systems

## Common Patterns
Patterns used in this component

## Configuration
Any configuration or settings

## Notes
Important considerations
```

### Template for Tier 3 (Feature Level)

```markdown
# [Feature Name] - Implementation Context

## Overview
What this feature does

## Implementation
How it's implemented

## Code Examples
Relevant code snippets

## Data Structures
Key data structures used

## Edge Cases
Known edge cases and how they're handled

## Performance Notes
Performance considerations

## Future Improvements
Potential enhancements
```

Use the provided templates:
- `docs/CONTEXT-tier2-component.md`
- `docs/CONTEXT-tier3-feature.md`

## Updating Documentation

### Manual Updates
Edit CONTEXT.md files directly when making changes

### Automated Updates (via Commands)
Once commands are set up:
```bash
/update-docs "path/to/CONTEXT.md"
```

The AI will:
1. Review recent changes in that area
2. Update the CONTEXT.md file
3. Ensure consistency with other docs

### Update Triggers

**Update Immediately**:
- New component or feature added
- Significant architectural change
- API changes
- New integration patterns

**Weekly Maintenance**:
- Review handoff.md
- Update recent changes
- Clean up obsolete notes

## Documentation Best Practices

### Do's
✅ Keep Tier 1 docs high-level and architecture-focused  
✅ Use Tier 2 for APIs and component interfaces  
✅ Save implementation details for Tier 3  
✅ Update docs immediately after major changes  
✅ Use clear, concise language  
✅ Include code examples in Tier 3  
✅ Link between related docs  

### Don'ts
❌ Don't duplicate information across tiers  
❌ Don't include obvious or self-documenting code  
❌ Don't let docs become outdated  
❌ Don't mix implementation details in Tier 1  
❌ Don't create CONTEXT.md for trivial features  

## Finding Information

### Quick Lookup
1. Check `docs/ai-context/docs-overview.md` for routing
2. Check component's CONTEXT.md for API details
3. Check `system-integration.md` for cross-component patterns

### Can't Find Context?
1. Look in parent component's CONTEXT.md
2. Check `docs/ai-context/system-integration.md`
3. Search relevant source files
4. Create new CONTEXT.md if needed

## Maintenance

### Regular Reviews
- **Weekly**: Update handoff.md with current state
- **After Features**: Update or create relevant CONTEXT.md
- **Before Releases**: Review all Tier 1 docs for accuracy

### Documentation Health Checks
- Are all major components documented? (Tier 2)
- Are complex features documented? (Tier 3)
- Is handoff.md current?
- Does docs-overview.md have all components listed?

## Additional Documentation

### Issue Tracking
`docs/open-issues/` - Track known issues and technical debt

### Specifications
`docs/specs/` - Feature specifications and design documents

### Coding Standards
`MCP-ASSISTANT-RULES.md` - Project-specific coding standards (for MCP/Gemini consultation)

## Commands Reference

Once the command system is set up, use these commands:

### Documentation Commands
- `/update-docs "[path]"` - Update specific documentation file
- `/create-docs "[path]"` - Create new CONTEXT.md file
- `/full-context "[task]"` - Load all relevant context and spawn sub-agents

### Development Commands
- `/code-review "[area]"` - Multi-agent code review
- `/refactor "[target]"` - Guided refactoring with context

See `.claude/commands/` for full command details (once created)

## Integration with AI Tools

### Claude Code
- Commands auto-load appropriate tiers
- Sub-agents inherit project context
- Hooks ensure security and consistency

### Other AI Tools
- Documentation structure works with any AI assistant
- Manually reference CLAUDE.md or relevant CONTEXT.md files
- Adapt commands as needed for different tools

## Support

**Questions about documentation system?**
- Review this README
- Check examples in existing CONTEXT.md files
- Refer to Claude Code Development Kit documentation

**Need to create new documentation?**
- Use provided templates
- Follow tier system guidelines
- Keep it concise and focused

---

**Last Updated**: 2025-10-29  
**Framework**: Claude Code Development Kit  
**Project**: Mai's Love Story









