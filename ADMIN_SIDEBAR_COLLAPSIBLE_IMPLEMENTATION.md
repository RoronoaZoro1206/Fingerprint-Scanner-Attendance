# Admin Sidebar Collapsible Implementation

## Overview
Reorganized the admin sidebar with collapsible groups to improve navigation and reduce clutter. Links are now organized into logical categories that can be expanded/collapsed.

## Organization Structure

### 1. Dashboard
- Overview (standalone link)

### 2. User Management (Collapsible)
- Manage Users
- Faculty
- Students
- Student ID Pool

### 3. Academic Setup (Collapsible)
- Academic Terms
- Departments
- Programs
- Courses

### 4. Class Management (Collapsible)
- Sections
- Rooms
- Classes
- Class Schedules

### 5. System (Collapsible)
- Register Fingerprint
- Audit Logs

### 6. Logout
- Logout (standalone link)

## Implementation

### 1. Updated View (_AdminSidebarPartial.cshtml)

#### Group Structure
```html
<div class="sidebar-group">
    <button class="sidebar-group-toggle" data-group="users">
        <i data-lucide="users"></i>
        <span>User Management</span>
        <i data-lucide="chevron-down" class="chevron"></i>
    </button>
    <div class="sidebar-group-content" data-group-content="users">
        <a href="..." class="sidebar-link sub-link">
            <i data-lucide="user-cog"></i>
            <span>Manage Users</span>
        </a>
        <!-- More sub-links -->
    </div>
</div>
```

#### Features
- Separate groups for desktop and mobile (different data-group IDs)
- Chevron icon rotates when expanded
- Sub-links have indentation and smaller icons
- Mobile version includes data-bs-dismiss="offcanvas"

### 2. CSS Styling (main-layout.css)

#### Group Toggle Button
```css
.sidebar-group-toggle {
    display: flex;
    align-items: center;
    padding: 0.75rem 1rem;
    width: 100%;
    background: transparent;
    border: none;
    font-weight: 500;
    cursor: pointer;
}
```

#### Collapsible Content
```css
.sidebar-group-content {
    max-height: 0;
    overflow: hidden;
    transition: max-height 0.3s ease;
}

.sidebar-group-content.expanded {
    max-height: 500px;
}
```

#### Chevron Animation
```css
.sidebar-group-toggle .chevron {
    transition: transform 0.2s ease;
}

.sidebar-group-toggle.expanded .chevron {
    transform: rotate(180deg);
}
```

#### Sub-link Styling
```css
.sidebar-link.sub-link {
    padding-left: 3rem;  /* Indented */
    font-size: 0.9rem;   /* Slightly smaller */
    font-weight: 400;    /* Normal weight */
}
```

### 3. JavaScript Functionality (sidebar.js)

#### Initialize Groups
```javascript
function initializeSidebarGroups() {
    const groupToggles = document.querySelectorAll('.sidebar-group-toggle');
    
    groupToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            const groupName = this.getAttribute('data-group');
            const content = document.querySelector(`[data-group-content="${groupName}"]`);
            
            // Toggle expanded class
            content.classList.toggle('expanded');
            this.classList.toggle('expanded');
        });
    });
    
    autoExpandActiveGroup();
}
```

#### Auto-Expand Active Group
```javascript
function autoExpandActiveGroup() {
    // Find active link
    // Expand its parent group automatically
    // User sees relevant section open on page load
}
```

## Features

### Collapsible Groups
- Click group header to expand/collapse
- Smooth animation (0.3s ease)
- Chevron rotates 180° when expanded
- Only one action needed to toggle

### Auto-Expand
- Automatically expands group containing current page
- User doesn't need to manually find their current section
- Improves navigation experience

### Visual Hierarchy
- **Group Headers**: Bold, larger icons
- **Sub-links**: Indented, smaller icons, lighter weight
- **Active Link**: Black background, white text
- **Hover**: Light gray background

### Mobile Support
- Same collapsible functionality on mobile
- Works with Bootstrap offcanvas
- Separate group IDs to avoid conflicts
- Auto-closes offcanvas after navigation

## Benefits

### Reduced Clutter
- **Before**: 15+ links in a long list
- **After**: 6 main items (2 standalone + 4 groups)
- Easier to scan and find items

### Better Organization
- Logical grouping by function
- Related items together
- Clear categories

### Improved UX
- Less scrolling required
- Faster navigation
- Auto-expand shows context
- Smooth animations

### Scalability
- Easy to add new links to existing groups
- Can add new groups without cluttering
- Maintains clean appearance

## User Experience

### First Visit
1. User sees collapsed groups
2. Clicks "User Management"
3. Group expands smoothly
4. Sub-links appear with animation
5. Chevron rotates down

### Navigation
1. User clicks "Faculty" link
2. Page loads
3. "User Management" group auto-expands
4. "Faculty" link highlighted
5. User knows where they are

### Mobile
1. User taps hamburger menu
2. Offcanvas sidebar opens
3. Same collapsible groups
4. Tap link → offcanvas closes → page loads

## Visual Design

### Collapsed State
```
📊 Overview
👥 User Management        ▼
📚 Academic Setup         ▼
📖 Class Management       ▼
⚙️  System                ▼
🚪 Logout
```

### Expanded State
```
📊 Overview
👥 User Management        ▲
    👤 Manage Users
    ✓ Faculty
    👥 Students
    💳 Student ID Pool
📚 Academic Setup         ▼
📖 Class Management       ▼
⚙️  System                ▼
🚪 Logout
```

## Technical Details

### Data Attributes
- `data-group="users"` - Identifies the toggle button
- `data-group-content="users"` - Identifies the content area
- Matching values link toggle to content

### CSS Classes
- `.sidebar-group` - Container
- `.sidebar-group-toggle` - Clickable header
- `.sidebar-group-content` - Collapsible content
- `.expanded` - Applied when open
- `.sub-link` - Nested link styling

### Transitions
- Max-height: 0.3s ease (smooth expand/collapse)
- Transform: 0.2s ease (chevron rotation)
- Background: 0.2s ease (hover effects)

## Browser Compatibility
- Modern browsers (Chrome, Firefox, Safari, Edge)
- CSS transitions supported
- JavaScript ES6+ features
- No external dependencies

## Performance
- Lightweight CSS animations
- No jQuery required
- Minimal JavaScript
- Fast rendering

## Accessibility
- Keyboard navigable
- Semantic HTML (button for toggle)
- Clear visual feedback
- Screen reader friendly

## Future Enhancements
- Remember expanded state in localStorage
- Keyboard shortcuts (e.g., Alt+1 for first group)
- Drag-and-drop to reorder groups
- Customizable grouping per user
- Search/filter within sidebar
- Badge counts on group headers

## Files Modified
1. `WebApplication1/Views/Shared/_AdminSidebarPartial.cshtml`
2. `WebApplication1/wwwroot/css/main-layout.css`
3. `WebApplication1/wwwroot/js/sidebar.js`

## Testing Checklist
- [ ] Groups expand/collapse on click
- [ ] Chevron rotates correctly
- [ ] Active link highlighted
- [ ] Parent group auto-expands
- [ ] Mobile offcanvas works
- [ ] Smooth animations
- [ ] No console errors
- [ ] Icons render correctly
- [ ] Links navigate properly
- [ ] Hover states work
