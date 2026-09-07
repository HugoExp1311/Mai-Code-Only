# Documentation Overview - Navigation Guide

This file serves as a **routing map** for AI assistants to quickly locate relevant context based on the task at hand.

## 🎯 Documentation Tiers

### Tier 1: Foundation (Always Auto-loaded)
**When**: Every AI session  
**Purpose**: Core project understanding

- `CLAUDE.md` - Master project context, technology stack, key features
- `docs/ai-context/project-structure.md` - Detailed architecture and file organization
- `docs/ai-context/system-integration.md` - How major systems interact
- `docs/ai-context/deployment-infrastructure.md` - Build and deployment context
- `docs/ai-context/handoff.md` - Session continuity and current state

### Tier 2: Component Context
**When**: Working with specific major systems  
**Purpose**: Deep dive into component architecture

| Component | Location | Use When |
|-----------|----------|----------|
| **Core Scripts** | `Assets/Scripts/CONTEXT.md` | Working on game logic, managers, controllers |
| **Live2D System** | `Assets/Live2D/CONTEXT.md` | Character animation, expression system |
| **Dialogue System** | `Assets/Scripts/DialogueSystem/CONTEXT.md` | Story content, dialogue flow |
| **Audio System** | `Audio System/CONTEXT.md` | FMOD integration, music, sound effects |
| **Localization** | `Assets/Localization/CONTEXT.md` | Multi-language support, translations |
| **UI System** | `Assets/Scripts/UI/CONTEXT.md` | User interface, menus, settings |
| **Editor Tools** | `Assets/Editor/CONTEXT.md` | Custom Unity editor extensions |

### Tier 3: Feature Context
**When**: Implementing or modifying specific features  
**Purpose**: Implementation details and patterns

| Feature | Location | Use When |
|---------|----------|----------|
| **Character Controller** | `Assets/Scripts/Characters/CONTEXT.md` | Character spawning, positioning, state management |
| **Save System** | `Assets/Scripts/SaveSystem/CONTEXT.md` | Save/load functionality, data persistence |
| **Scene Management** | `Assets/Scripts/SceneManagement/CONTEXT.md` | Scene transitions, loading screens |
| **Input Handling** | `Assets/Scripts/Input/CONTEXT.md` | Player input, controls, input mapping |
| **Settings System** | `Assets/Scripts/Settings/CONTEXT.md` | Game settings, preferences, audio/video options |

## 🔧 MCP Integration

This project uses **MCP (Model Context Protocol)** tools for efficient, token-optimized development. MCP tools enable on-demand code access and documentation retrieval.

### MCP-First Approach

**MANDATORY**: Use MCP tools as the primary method for all code operations.

**Serena MCP Tools** (Symbol-based code operations):
- `mcp_serena_find_file` - Locate files by name or pattern
- `mcp_serena_get_symbols_overview` - Get file structure overview
- `mcp_serena_find_symbol` - Find specific classes, methods, properties
- `mcp_serena_replace_symbol_body` - Edit code at symbol level
- `mcp_serena_insert_after_symbol` / `mcp_serena_insert_before_symbol` - Add new code
- `mcp_serena_search_for_pattern` - Search for code patterns
- `mcp_serena_find_referencing_symbols` - Find where symbols are used
- `mcp_serena_read_memory` / `mcp_serena_write_memory` - Persist project knowledge

**Context7 MCP Tools** (On-demand documentation):
- `mcp_context7_resolve_library_id` - Find Unity, Live2D, FMOD library IDs
- `mcp_context7_get_library_docs` - Fetch API documentation with specific topics

### When to Use MCP vs Loading Documentation

**Use MCP Tools For**:
- ✅ Finding and reading code files
- ✅ Editing specific methods or classes
- ✅ Searching for patterns or references
- ✅ Fetching Unity/Live2D/FMOD API documentation
- ✅ Storing and retrieving project knowledge

**Load Tier 2 CONTEXT.md When**:
- 📘 You need component architecture understanding
- 📘 You need to understand how systems integrate
- 📘 You're working across multiple files in a component
- 📘 You need to understand design decisions and patterns
- 📘 Task complexity is STANDARD or DEEP level

**Load Tier 3 CONTEXT.md When**:
- 📗 You need feature implementation details
- 📗 You need to understand specific algorithms or patterns
- 📗 You're modifying complex feature logic
- 📗 You need edge case handling information
- 📗 Task complexity is DEEP level

**Progressive Loading Strategy**:
```
MINIMAL (Simple tasks):
  → Session Guideline + MCP tools only
  → No CONTEXT.md loading needed
  → Example: Fix typo, small bug fix

STANDARD (Medium tasks):
  → Session Guideline + MCP tools
  → Load relevant Tier 2 CONTEXT.md
  → Use Context7 for API docs
  → Example: Add feature, refactor component

DEEP (Complex tasks):
  → Session Guideline + MCP tools
  → Load multiple Tier 2 CONTEXT.md files
  → Load relevant Tier 3 CONTEXT.md files
  → Review system-integration.md
  → Example: Architectural change, multi-system integration
```

## 📋 Task-Based Context Loading Guide

### Working on Dialogue Features
**MCP First**:
- Use `mcp_serena_find_file` to locate DialogueManager.cs
- Use `mcp_serena_get_symbols_overview` to see structure
- Use `mcp_serena_find_symbol` to read specific methods

**Load if Needed**:
- Tier 2: `Assets/Scripts/DialogueSystem/CONTEXT.md` (for architecture)
- Tier 3: `Assets/Localization/CONTEXT.md` (if involving translations)

### Working on Character Animations
**MCP First**:
- Use `mcp_serena_find_file` to locate character animation scripts
- Use `mcp_serena_find_symbol` to read relevant classes/methods
- Use Context7 to fetch Live2D Cubism SDK documentation

**Load if Needed**:
- Tier 2: `Assets/Live2D/CONTEXT.md` (for Live2D integration architecture)
- Tier 3: `Assets/Scripts/Characters/CONTEXT.md` (for character controller patterns)

### Working on Audio System
**MCP First**:
- Use `mcp_serena_find_file` to locate audio manager scripts
- Use Context7 to fetch FMOD Unity integration documentation
- Use `mcp_serena_find_symbol` for specific audio methods

**Load if Needed**:
- Tier 2: `Audio System/CONTEXT.md` (for FMOD architecture)

### Working on UI/Menus
**MCP First**:
- Use `mcp_serena_find_file` to locate UI scripts
- Use `mcp_serena_get_symbols_overview` to understand UI structure
- Use Context7 to fetch Unity UI documentation

**Load if Needed**:
- Tier 2: `Assets/Scripts/UI/CONTEXT.md` (for UI architecture)
- Tier 3: `Assets/Scripts/Settings/CONTEXT.md` (if settings-related)

### Adding New Features
**MCP First**:
- Use `mcp_serena_find_file` to explore related components
- Use `mcp_serena_search_for_pattern` to find similar implementations
- Use `mcp_serena_read_memory` to check for existing patterns

**Load if Needed**:
- Tier 2: Relevant component contexts based on feature type
- Review: `docs/ai-context/system-integration.md` for integration patterns

### Bug Fixes
**MCP First**:
- Use `mcp_serena_find_file` to locate affected file
- Use `mcp_serena_find_symbol` to read specific buggy method
- Use `mcp_serena_find_referencing_symbols` to check usage

**Load if Needed**:
- Tier 2: Component context only if bug involves architecture
- Tier 3: Feature context only if bug is in complex feature logic

### Refactoring
**MCP First**:
- Use `mcp_serena_get_symbols_overview` to understand current structure
- Use `mcp_serena_find_referencing_symbols` to check dependencies
- Use `mcp_serena_search_for_pattern` to find related code

**Load if Needed**:
- Tier 2: All affected component contexts
- Review: `docs/ai-context/system-integration.md` for dependencies

### Code Review
**MCP First**:
- Use `mcp_serena_find_symbol` to read reviewed code
- Use `mcp_serena_find_referencing_symbols` to check usage patterns

**Load if Needed**:
- Tier 2: Context for reviewed component
- Review: `MCP-ASSISTANT-RULES.md` for coding standards

## 📝 Documentation Maintenance

### When to Update Documentation

**Update Immediately After**:
- Major architectural changes
- New system/component additions
- Changing integration patterns
- Adding new dependencies

**Weekly Reviews**:
- Ensure `handoff.md` reflects current state
- Update component contexts with recent learnings
- Add new feature contexts as needed

### Documentation Update Command
Use the custom command:
```
/update-docs "[path-to-context-file]"
```

Example:
```
/update-docs "Assets/Scripts/DialogueSystem/CONTEXT.md"
```

## 🎓 Documentation Best Practices

### For Component Context (Tier 2)
- **Focus**: Architecture, responsibilities, key classes
- **Include**: Public APIs, event systems, data flows
- **Avoid**: Implementation details (those go in Tier 3)

### For Feature Context (Tier 3)
- **Focus**: Implementation patterns, algorithms, edge cases
- **Include**: Code examples, configuration details
- **Avoid**: Obvious or self-documenting code

### For Foundation Docs (Tier 1)
- **Focus**: High-level understanding, system overview
- **Include**: Technology decisions, project goals
- **Avoid**: Deep technical details (those belong in Tier 2/3)

## 🔍 Quick Reference

**New to project?** → Read Session Guideline + use MCP tools to explore  
**Simple task?** → MCP tools only (MINIMAL context)  
**Working on specific feature?** → MCP tools + relevant Tier 2 (STANDARD context)  
**Deep implementation?** → MCP tools + Tier 2 + Tier 3 (DEEP context)  
**Cross-system work?** → MCP tools + multiple Tier 2 contexts (DEEP context)  
**Need API docs?** → Use Context7 MCP tools  
**Need coding standards?** → `MCP-ASSISTANT-RULES.md`  
**Need project knowledge?** → Use `mcp_serena_list_memories` and `mcp_serena_read_memory`  

## 🚨 Common Issues

**Can't find relevant context?**
1. Use `mcp_serena_find_file` to locate files
2. Use `mcp_serena_search_for_pattern` to search for code patterns
3. Check this overview file for CONTEXT.md routing
4. Use `mcp_serena_list_memories` to check stored project knowledge
5. Look in parent component's CONTEXT.md
6. Search `docs/ai-context/system-integration.md` for cross-component info

**Documentation seems outdated?**
1. Use `/update-docs` command
2. Check `handoff.md` for recent changes
3. Review git commit history
4. Use MCP tools to read current code directly

**Multiple contexts overlap?**
- This is intentional - different perspectives help
- Choose based on your current focus level
- When in doubt, start with MCP tools (MINIMAL context)
- Escalate to STANDARD or DEEP context only if needed

**MCP tools not working?**
1. Check if file/symbol path is correct
2. Try `substring_matching=true` for find_symbol
3. Use `mcp_serena_get_symbols_overview` to see available symbols
4. Fall back to `mcp_serena_read_file` as last resort
5. See `.kiro/mcp-workflow-examples.md` for troubleshooting

---

**Last Updated**: 2025-10-29  
**Maintained By**: AI-assisted development workflow









