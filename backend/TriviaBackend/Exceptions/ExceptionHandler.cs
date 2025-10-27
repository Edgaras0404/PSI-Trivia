namespace TriviaBackend.Exceptions
{
    public class ExceptionHandler(RequestDelegate next, ILogger<ExceptionHandler> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (PlayerNotFoundException)
            {

            }
            catch (Exception)
            {

            }
        }
    }
}
