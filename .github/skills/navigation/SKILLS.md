# Navigation

The main menu uses a view stack for consistent sub-menu navigation:

| View | Description |
|------|-------------|
| **Main Menu** | Server overview table with top-level actions |
| **Group Detail** | Select a group → view its servers, connect all, or select a server |
| **Server Actions** | Select a server → Connect, Edit, Delete, or Back |

All sub-menus include a `[[B]] Back` option to return to the previous view. The Add Server form supports cancellation by leaving the Alias field empty. The Create Group form supports cancellation by leaving the name empty.

## Related Services

- `INavigationService` — View stack manager for sub-menu navigation
- `IGroupMenuService` — Group browsing and group detail sub-menus
- `IServerMenuService` — Server selection and server action sub-menus
