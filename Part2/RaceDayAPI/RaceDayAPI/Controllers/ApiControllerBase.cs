using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    // helpers that read the logged-in user from the cookie claims.
    public abstract class ApiControllerBase : ControllerBase
    {
        protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        // The id that Login stored in the cookie 
        protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // True when the caller is an Organiser AND is the organiser who owns the record.
        protected bool IsOwnerOrganiser(int organiserId) =>
            IsAuthenticated && User.IsInRole(Roles.Organiser) && CurrentUserId == organiserId;
    }
}
