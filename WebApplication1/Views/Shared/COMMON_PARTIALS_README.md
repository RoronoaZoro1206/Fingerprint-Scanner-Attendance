# Common Partial Views Documentation

This document explains the common partial views used for including shared CSS and JavaScript files across all modules.

## Overview

The application now uses centralized partial views to include common CSS and JavaScript files, ensuring consistency and easier maintenance across Admin, Faculty, and Student modules.

## Partial Views

### 1. `_HeadPartial.cshtml`
**Location:** `Views/Shared/_HeadPartial.cshtml`

**Purpose:** Includes all CSS files needed in the `<head>` section of pages.

**Contents:**
- Core library CSS (Bootstrap, Toastify, DataTables)
- Site-wide CSS
- Common styles partial (`_CommonStyles.cshtml`)

**Usage:**
```razor
<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <title>@ViewData["Title"] - School Attendance System</title>
    
    @await Html.PartialAsync("_HeadPartial")
    
    <!-- Page-specific CSS can be added here -->
    <link rel="stylesheet" href="~/css/specific-page.css" asp-append-version="true"/>
</head>
```

### 2. `_CommonStyles.cshtml`
**Location:** `Views/Shared/_CommonStyles.cshtml`

**Purpose:** Includes all common CSS files shared across modules.

**Included Files:**
- `buttons.css` - Button styles
- `page-layout.css` - Page layout components
- `cards.css` - Card components
- `tables.css` - Table and DataTables styles
- `forms.css` - Form elements
- `modals.css` - Modal dialogs
- `badges.css` - Status badges
- `dropdowns.css` - Dropdown menus
- `states.css` - Loading/empty states
- `utilities.css` - Utility classes

**Usage:**
This partial is automatically included via `_HeadPartial.cshtml`. You can also include it directly:
```razor
@await Html.PartialAsync("_CommonStyles")
```

### 3. `_LayoutScripts.cshtml`
**Location:** `Views/Shared/_LayoutScripts.cshtml`

**Purpose:** Includes all JavaScript files needed at the end of the `<body>` section.

**Contents:**
- Core library JS (jQuery, Bootstrap, Toastify, DataTables, PDFMake)
- Lucide icons initialization
- Site-wide JS
- Common scripts partial (`_CommonScripts.cshtml`)

**Usage:**
```razor
<body>
    <!-- Page content -->
    
    @await Html.PartialAsync("_LayoutScripts")
    
    <!-- Page-specific scripts can be added here -->
    @await RenderSectionAsync("Scripts", required: false)
</body>
```

### 4. `_CommonScripts.cshtml`
**Location:** `Views/Shared/_CommonScripts.cshtml`

**Purpose:** Includes all common JavaScript utility files shared across modules.

**Included Files:**
- `form-validation.js` - Form validation utilities
- `modal-utils.js` - Modal handling functions
- `export-utils.js` - Export functions (print, CSV, Excel, PDF)
- `datatable-init.js` - DataTable initialization helpers
- `ajax-utils.js` - AJAX request wrappers
- `ui-utils.js` - UI helper functions

**Usage:**
This partial is automatically included via `_LayoutScripts.cshtml`. You can also include it directly:
```razor
@await Html.PartialAsync("_CommonScripts")
```

## File Structure

```
Views/
└── Shared/
    ├── _HeadPartial.cshtml          (Main CSS partial)
    ├── _CommonStyles.cshtml          (Common CSS files)
    ├── _LayoutScripts.cshtml         (Main JS partial)
    ├── _CommonScripts.cshtml         (Common JS files)
    ├── _MainLayout.cshtml            (Main layout using above partials)
    └── COMMON_PARTIALS_README.md     (This file)

wwwroot/
├── css/
│   └── common/                       (Common CSS files)
│       ├── buttons.css
│       ├── page-layout.css
│       ├── cards.css
│       ├── tables.css
│       ├── forms.css
│       ├── modals.css
│       ├── badges.css
│       ├── dropdowns.css
│       ├── states.css
│       └── utilities.css
└── js/
    └── common/                       (Common JS files)
        ├── form-validation.js
        ├── modal-utils.js
        ├── export-utils.js
        ├── datatable-init.js
        ├── ajax-utils.js
        └── ui-utils.js
```

## Current Implementation

The `_MainLayout.cshtml` already uses these partials:

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <title>@ViewData["Title"] - School Attendance System</title>
    
    @await Html.PartialAsync("_HeadPartial")
    
    <link rel="stylesheet" href="~/css/main-layout.css" asp-append-version="true"/>
    <link rel="stylesheet" href="~/css/fingerprint-prompt.css" asp-append-version="true"/>
</head>
<body>
    <!-- Page content -->
    
    @await Html.PartialAsync("_LayoutScripts")
    <script src="~/js/fingerprint-prompt.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

## Benefits

### 1. Centralized Management
- Update common files in one place
- All pages automatically get updates
- Easier to maintain and debug

### 2. Consistency
- All modules use the same styles and utilities
- Consistent look and feel across the application
- Reduced code duplication

### 3. Performance
- Browser can cache common files
- Smaller page-specific files
- Faster page loads

### 4. Easier Development
- Clear separation of common vs. page-specific code
- Reusable utilities available everywhere
- Less code to write for new pages

## Adding Page-Specific Styles/Scripts

### For Individual Pages

Add page-specific CSS after the common partials:

```razor
@section Styles {
    <link rel="stylesheet" href="~/css/admin/specific-page.css" asp-append-version="true"/>
}
```

Add page-specific JavaScript in the Scripts section:

```razor
@section Scripts {
    <script src="~/js/admin/specific-page.js" asp-append-version="true"></script>
}
```

### For Role-Specific Layouts

If you create role-specific layouts (e.g., `_AdminLayout.cshtml`), include the common partials:

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    @await Html.PartialAsync("_HeadPartial")
    
    <!-- Admin-specific CSS -->
    <link rel="stylesheet" href="~/css/admin/admin-common.css" asp-append-version="true"/>
    
    @RenderSection("Styles", required: false)
</head>
<body>
    @RenderBody()
    
    @await Html.PartialAsync("_LayoutScripts")
    
    <!-- Admin-specific JS -->
    <script src="~/js/admin/admin-common.js" asp-append-version="true"></script>
    
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

## Load Order

The files are loaded in this order:

### CSS Load Order:
1. Bootstrap CSS
2. Site CSS
3. Toastify CSS
4. DataTables CSS
5. **Common CSS files** (via `_CommonStyles.cshtml`)
6. Layout-specific CSS (e.g., `main-layout.css`)
7. Page-specific CSS (via `@section Styles`)

### JavaScript Load Order:
1. jQuery
2. Bootstrap JS
3. Toastify JS
4. Toast utilities
5. Site JS
6. Sidebar JS
7. Lucide icons
8. PDFMake
9. DataTables
10. **Common JS utilities** (via `_CommonScripts.cshtml`)
11. Layout-specific JS (e.g., `fingerprint-prompt.js`)
12. Page-specific JS (via `@section Scripts`)

This order ensures that:
- Dependencies are loaded before dependent code
- Common utilities are available to all page-specific code
- Page-specific code can override common styles if needed

## Troubleshooting

### Common Issues

**Issue:** Common styles not applying
- **Solution:** Ensure `_HeadPartial.cshtml` is included in your layout
- **Check:** Browser developer tools to verify CSS files are loaded

**Issue:** Common utilities not available
- **Solution:** Ensure `_LayoutScripts.cshtml` is included before page-specific scripts
- **Check:** Browser console for JavaScript errors

**Issue:** Styles being overridden
- **Solution:** Check CSS specificity and load order
- **Tip:** Page-specific CSS loads after common CSS, so it can override

### Debugging

To verify files are loaded correctly:

1. Open browser developer tools (F12)
2. Go to Network tab
3. Reload the page
4. Filter by CSS or JS
5. Verify all common files are loaded with 200 status

## Migration Guide

If you have existing pages not using these partials:

1. **Replace direct CSS includes** with `_HeadPartial.cshtml`:
   ```razor
   <!-- Before -->
   <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css"/>
   <link rel="stylesheet" href="~/css/site.css"/>
   <!-- ... many more ... -->
   
   <!-- After -->
   @await Html.PartialAsync("_HeadPartial")
   ```

2. **Replace direct JS includes** with `_LayoutScripts.cshtml`:
   ```razor
   <!-- Before -->
   <script src="~/lib/jquery/dist/jquery.min.js"></script>
   <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
   <!-- ... many more ... -->
   
   <!-- After -->
   @await Html.PartialAsync("_LayoutScripts")
   ```

3. **Remove duplicate common CSS** from page-specific files

4. **Update JavaScript** to use common utilities

## Best Practices

1. **Always use the partials** - Don't include common files directly
2. **Keep page-specific code separate** - Only include what's unique to that page
3. **Use common utilities** - Leverage the shared JavaScript functions
4. **Test after changes** - Verify all pages still work correctly
5. **Document customizations** - If you override common styles, document why

## Support

For questions or issues:
- Review the common CSS/JS README files in `wwwroot/css/common/` and `wwwroot/js/common/`
- Check the `COMMON_REFACTORING_GUIDE.md` for implementation details
- Test changes in development before deploying to production
