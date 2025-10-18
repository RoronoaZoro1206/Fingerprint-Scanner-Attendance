# Common CSS Files

This directory contains shared CSS files used across Admin, Faculty, and Student modules to eliminate code duplication and improve maintainability.

## Files

### Core Styles
- **buttons.css** - All button styles (primary, secondary, action buttons, etc.)
- **page-layout.css** - Page header and layout components
- **cards.css** - Card components and statistics cards
- **tables.css** - Table and DataTables styling
- **forms.css** - Form elements and validation styles
- **modals.css** - Modal dialog styles
- **badges.css** - Status badges, count badges, and role badges
- **dropdowns.css** - Dropdown menu styles
- **states.css** - Loading, empty, and error states
- **utilities.css** - Utility classes for spacing, display, etc.

## Usage

Include these common CSS files in your layout files before role-specific CSS:

```html
<!-- Common Styles -->
<link rel="stylesheet" href="~/css/common/buttons.css" />
<link rel="stylesheet" href="~/css/common/page-layout.css" />
<link rel="stylesheet" href="~/css/common/cards.css" />
<link rel="stylesheet" href="~/css/common/tables.css" />
<link rel="stylesheet" href="~/css/common/forms.css" />
<link rel="stylesheet" href="~/css/common/modals.css" />
<link rel="stylesheet" href="~/css/common/badges.css" />
<link rel="stylesheet" href="~/css/common/dropdowns.css" />
<link rel="stylesheet" href="~/css/common/states.css" />
<link rel="stylesheet" href="~/css/common/utilities.css" />

<!-- Role-specific styles -->
<link rel="stylesheet" href="~/css/admin/specific-page.css" />
```

## Benefits

1. **Reduced Duplication** - Common styles are defined once and reused
2. **Easier Maintenance** - Update styles in one place
3. **Consistency** - Ensures consistent look and feel across all modules
4. **Smaller File Sizes** - Less redundant CSS code
5. **Better Organization** - Clear separation of common vs. role-specific styles

## Customization

Role-specific customizations should be placed in their respective directories:
- `wwwroot/css/admin/` - Admin-specific styles
- `wwwroot/css/faculty/` - Faculty-specific styles
- `wwwroot/css/student/` - Student-specific styles

These role-specific files can override common styles when needed.
