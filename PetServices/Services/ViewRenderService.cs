using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Threading.Tasks;

public static class RazorViewToStringRenderer
{
    public static async Task<string> RenderViewAsync(this Controller controller, string viewName, object model, bool partial = false)
    {
        controller.ViewData.Model = model;

        using var sw = new StringWriter();
        var actionContext = new ActionContext(controller.HttpContext, controller.RouteData, controller.ControllerContext.ActionDescriptor);
        var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
        var viewResult = viewEngine.FindView(actionContext, viewName, !partial);

        if (!viewResult.Success)
            throw new FileNotFoundException($"View {viewName} not found.");

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            controller.ViewData,
            controller.TempData,
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
}
