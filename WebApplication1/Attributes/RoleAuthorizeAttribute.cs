using Fingerprint_Attendance_System.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fingerprint_Attendance_System.Attributes
{
    /// <summary>
    /// Custom authorization attribute to enforce role-based access control
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly USER_ROLES[] _allowedRoles;

        public RoleAuthorizeAttribute(params USER_ROLES[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var userId = session.GetInt32("UserId");
            var roleId = session.GetInt32("RoleId");

            // Check if user is authenticated
            if (!userId.HasValue || !roleId.HasValue)
            {
                // Redirect to login page
                context.Result = new RedirectToActionResult("Index", "Auth", null);
                return;
            }

            // Check if user has the required role
            var userRole = (USER_ROLES)roleId.Value;
            if (!_allowedRoles.Contains(userRole))
            {
                // Redirect to access denied page
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }
        }
    }
}
