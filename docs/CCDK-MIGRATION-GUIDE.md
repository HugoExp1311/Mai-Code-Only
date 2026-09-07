# CCDK Migration Guide
# Transitioning to Claude Code Development Kit

## Overview

This guide helps you transition from the previous documentation structure to the new **Claude Code Development Kit (CCDK)** system.

---

## What Changed?

### Before (Old Structure)
```
Mai-s-Love-Story/
├── README.md (minimal)
├── CLAUDE.md (basic context)
├── docs/
│   ├── README.md (documentation guide)
│   └── Various scattered docs
└── No centralized comprehensive guide
```

### After (New CCDK Structure)
```
Mai-s-Love-Story/
├── README.md (enhanced with links)
├── CLAUDE.md (master context - Tier 1)
├── CCDK.md (comprehensive project guide) ⭐ NEW
├── QUICK-REFERENCE.md (quick reference card) ⭐ NEW
├── docs/
│   ├── README.md (updated with CCDK references)
│   ├── ARCHITECTURE-DIAGRAM.md (visual diagrams) ⭐ NEW
│   ├── CCDK-MIGRATION-GUIDE.md (this file) ⭐ NEW
│   └── Existing documentation
└── Centralized, comprehensive documentation
```

---

## Key Improvements

### 1. Comprehensive Project Guide (CCDK.md)

**What it is**: A complete, self-contained guide covering:
- Project overview and technology stack
- Architecture patterns and design decisions
- MCP-first development workflow
- Documentation system (3-tier)
- All key systems with code examples
- Common tasks and workflows
- Testing and deployment
- Quick reference sections

**When to use**: 
- New developers joining the project
- AI assistants needing comprehensive context
- Reference for architectural decisions
- Learning project patterns and conventions

### 2. Quick Reference Card (QUICK-REFERENCE.md)

**What it is**: A condensed, scannable reference with:
- Key file locations
- MCP tool commands
- Common code patterns
- Debugging checklists
- Quick task guides

**When to use**:
- Quick lookups during development
- Refreshing memory on common patterns
- Finding file locations quickly
- Debugging common issues

### 3. Architecture Diagrams (docs/ARCHITECTURE-DIAGRAM.md)

**What it is**: Visual Mermaid diagrams showing:
- System overview
- Data flow
- UI panel hierarchy
- Character system architecture
- Dialogue system flow
- Localization system
- Audio integration
- Asset loading flow

**When to use**:
- Understanding system relationships
- Visualizing data flow
- Planning architectural changes
- Onboarding new developers

### 4. Enhanced README.md

**What changed**:
- Added clear documentation links
- Project overview with features
- Quick start guide for developers and AI
- Technology stack summary
- Key systems overview
- Contributing guidelines

**Impact**: First-time visitors get immediate guidance

### 5. Updated CLAUDE.md

**What changed**:
- Added reference to CCDK.md
- Maintained as Tier 1 foundation document
- Streamlined for session initialization

**Impact**: Clearer separation between session context and comprehensive guide

---

## Migration Checklist

### For Developers

- [ ] Read [CCDK.md](../CCDK.md) to understand the complete project
- [ ] Bookmark [QUICK-REFERENCE.md](../QUICK-REFERENCE.md) for daily use
- [ ] Review [ARCHITECTURE-DIAGRAM.md](ARCHITECTURE-DIAGRAM.md) for visual understanding
- [ ] Update any personal notes to reference new documentation
- [ ] Share CCDK.md with new team members

### For AI Assistants

- [ ] Session Guideline (`.kiro/session-guideline.md`) auto-loads at start
- [ ] Reference CCDK.md for comprehensive project understanding
- [ ] Use QUICK-REFERENCE.md for quick lookups
- [ ] Follow MCP-first development workflow from CCDK.md
- [ ] Apply progressive context loading strategy (MINIMAL → STANDARD → DEEP)

### For Project Maintenance

- [ ] Keep CCDK.md updated with architectural changes
- [ ] Update QUICK-REFERENCE.md when adding common patterns
- [ ] Add new diagrams to ARCHITECTURE-DIAGRAM.md as systems evolve
- [ ] Maintain Tier 2/3 CONTEXT.md files in component directories
- [ ] Update README.md when adding major features

---

## Documentation Hierarchy (Clarified)

### Tier 1: Foundation (Always Loaded)
**Files**:
- `CLAUDE.md` - Master project context
- `.kiro/session-guideline.md` - Session initialization

**Purpose**: Essential context for every AI session

**When to update**: After major architectural changes

### Tier 2: Component Context (Load for Medium Tasks)
**Files**:
- `Assets/Scripts/UI/CONTEXT.md`
- `Assets/Scripts/Base/Character/CONTEXT.md`
- Other component-level CONTEXT.md files

**Purpose**: Deep understanding of specific systems

**When to update**: After component architecture changes

### Tier 3: Feature Context (Load for Complex Tasks)
**Files**:
- `Assets/Scripts/Shop/CONTEXT.md`
- `Assets/Scripts/Inventory/CONTEXT.md`
- Other feature-level CONTEXT.md files

**Purpose**: Implementation details for specific features

**When to update**: After feature implementation changes

### Reference Documentation (Use as Needed)
**Files**:
- `CCDK.md` - Comprehensive project guide
- `QUICK-REFERENCE.md` - Quick reference card
- `docs/ARCHITECTURE-DIAGRAM.md` - Visual diagrams
- `docs/README.md` - Documentation system guide

**Purpose**: Learning, reference, onboarding

**When to update**: As project evolves

---

## Workflow Changes

### Old Workflow
```
1. Load CLAUDE.md
2. Search for relevant docs
3. Load multiple scattered files
4. Piece together understanding
5. Start coding
```

### New Workflow (MCP-First)
```
1. Session Guideline auto-loads (Tier 1)
2. Use MCP tools to explore code
3. Load CCDK.md if comprehensive understanding needed
4. Load Tier 2/3 CONTEXT.md only if needed
5. Use QUICK-REFERENCE.md for quick lookups
6. Start coding with MCP tools
```

**Key Difference**: Progressive context loading, MCP-first approach, centralized comprehensive guide

---

## Common Questions

### Q: Should I read CCDK.md every session?
**A**: No. CCDK.md is a reference guide. Session Guideline auto-loads essential context. Load CCDK.md when you need comprehensive understanding or are learning the project.

### Q: What's the difference between CLAUDE.md and CCDK.md?
**A**: 
- **CLAUDE.md**: Tier 1 foundation, auto-loaded, essential session context
- **CCDK.md**: Comprehensive reference guide, load when needed, complete project documentation

### Q: When should I use QUICK-REFERENCE.md vs CCDK.md?
**A**:
- **QUICK-REFERENCE.md**: Quick lookups, common patterns, debugging
- **CCDK.md**: Learning, comprehensive understanding, architectural decisions

### Q: Do I still need to read Tier 2/3 CONTEXT.md files?
**A**: Yes, but only when needed:
- **Tier 2**: Load for medium tasks requiring component-specific knowledge
- **Tier 3**: Load for complex tasks requiring deep feature understanding

### Q: How do I keep documentation updated?
**A**:
1. Update CCDK.md for architectural changes
2. Update QUICK-REFERENCE.md for new common patterns
3. Update Tier 2/3 CONTEXT.md for component/feature changes
4. Update ARCHITECTURE-DIAGRAM.md for new systems
5. Update README.md for major features

### Q: What if I find outdated information?
**A**: Update the relevant file immediately:
- Architectural changes → CCDK.md
- Common patterns → QUICK-REFERENCE.md
- Component changes → Tier 2 CONTEXT.md
- Feature changes → Tier 3 CONTEXT.md

---

## Benefits of CCDK

### For Developers
✅ Single comprehensive guide (CCDK.md)  
✅ Quick reference for daily tasks (QUICK-REFERENCE.md)  
✅ Visual architecture understanding (ARCHITECTURE-DIAGRAM.md)  
✅ Clear documentation hierarchy  
✅ Faster onboarding for new team members  

### For AI Assistants
✅ Progressive context loading (token optimization)  
✅ MCP-first workflow (efficient code operations)  
✅ Clear guidance on when to load what  
✅ Comprehensive reference when needed  
✅ Consistent project understanding across sessions  

### For Project Maintenance
✅ Centralized documentation updates  
✅ Clear update triggers  
✅ Reduced documentation drift  
✅ Better knowledge preservation  
✅ Easier to maintain consistency  

---

## Next Steps

1. **Read CCDK.md**: Get comprehensive project understanding
2. **Bookmark QUICK-REFERENCE.md**: Use for daily development
3. **Review ARCHITECTURE-DIAGRAM.md**: Visualize system relationships
4. **Update workflows**: Adopt MCP-first development approach
5. **Share with team**: Ensure everyone uses new documentation

---

## Feedback & Improvements

Found issues or have suggestions? Update this guide or create an issue in the project tracker.

---

**Last Updated**: 2025-11-13  
**Related Documentation**:
- [CCDK.md](../CCDK.md) - Comprehensive project guide
- [QUICK-REFERENCE.md](../QUICK-REFERENCE.md) - Quick reference card
- [ARCHITECTURE-DIAGRAM.md](ARCHITECTURE-DIAGRAM.md) - Visual diagrams
- [README.md](../README.md) - Project overview
