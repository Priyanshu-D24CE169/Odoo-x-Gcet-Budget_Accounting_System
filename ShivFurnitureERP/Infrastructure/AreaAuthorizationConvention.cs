using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace ShivFurnitureERP.Infrastructure;

public class AreaAuthorizationConvention : IApplicationModelConvention
{
    private readonly string _area;
    private readonly string _policy;

    public AreaAuthorizationConvention(string area, string policy)
    {
        _area = area;
        _policy = policy;
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var areaAttribute = controller.Attributes.OfType<AreaAttribute>().FirstOrDefault()
                                ?? controller.ControllerType.GetCustomAttributes(typeof(AreaAttribute), true).OfType<AreaAttribute>().FirstOrDefault();

            if (areaAttribute is null)
            {
                continue;
            }

            if (!string.Equals(areaAttribute.RouteValue, _area, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (AllowsAnonymous(controller))
            {
                continue;
            }

            controller.Filters.Add(new AuthorizeFilter(_policy));
        }
    }

    private static bool AllowsAnonymous(ControllerModel controller)
    {
        if (controller.Attributes.OfType<IAllowAnonymousFilter>().Any())
        {
            return true;
        }

        return controller.Filters.OfType<IAllowAnonymousFilter>().Any();
    }
}
