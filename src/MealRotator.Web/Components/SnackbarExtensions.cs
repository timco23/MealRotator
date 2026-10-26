using MudBlazor;

namespace MealRotator.Web.Components;

public static class SnackbarExtensions
{
    /// <summary>
    /// Runs a service action. A user error (<see cref="InvalidOperationException"/>) is shown
    /// verbatim as an error message; on success an optional short confirmation is shown.
    /// </summary>
    /// <returns><c>true</c> if the action succeeded.</returns>
    public static async Task<bool> RunAsync(this ISnackbar snackbar, Func<Task> action, string? successMessage = null)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException ex)
        {
            snackbar.Add(ex.Message, Severity.Error);
            return false;
        }

        if (successMessage is not null)
            snackbar.Add(successMessage, Severity.Success);

        return true;
    }
}
