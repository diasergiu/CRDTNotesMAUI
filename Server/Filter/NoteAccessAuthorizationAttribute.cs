using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Server.ServeRepositories;

namespace Server.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NoteAccessAuthorizationAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _noteIdParameterName;
        public NoteAccessAuthorizationAttribute(string noteIdParameterName = "noteId")
        {
            _noteIdParameterName = noteIdParameterName;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Extract userId from header
            var userIdHeader = context.HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
            if (!Guid.TryParse(userIdHeader, out var userId)) // do we do this twice, once in the middleware and once here
            {
                context.Result = new BadRequestObjectResult("Invalid or missing X-User-Id header.");
                return;
            }

            // Extract noteId from route or query parameters
            if (!context.ActionArguments.TryGetValue(_noteIdParameterName, out var noteIdObj) ||
               !(noteIdObj is Guid noteId) || noteId == Guid.Empty)
            {
                context.Result = new BadRequestObjectResult($"Note ID (parameter: {_noteIdParameterName}) not found or invalid.");
                return;
            }

            // Get the repository from DI
            var notesRepository = context.HttpContext.RequestServices.GetService<NotesRepository>();
            if (notesRepository == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Check access
            if (!notesRepository.DoseUserHaveAccessToNote(noteId, userId))
            {
                context.Result = new BadRequestObjectResult("You do not have access to this note.");
                return;
            }

            // User has access, continue to action
            await next();
        }
    }
}