using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace iLearn.API.Extensions
{
    public static class AuthorizationPolicyAuditExtensions
    {
        public static WebApplication ValidateExplicitControllerAuthorizationPolicies(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var actionProvider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

            var violations = actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(action => action.ControllerTypeInfo.Assembly == typeof(AuthorizationPolicyAuditExtensions).Assembly)
                .Where(action => !HasAllowAnonymous(action))
                .Where(action => !HasNamedPolicy(action))
                .Select(action => $"{action.ControllerName}.{action.ActionName} ({action.AttributeRouteInfo?.Template ?? action.DisplayName})")
                .OrderBy(name => name)
                .ToList();

            if (violations.Count > 0)
            {
                throw new InvalidOperationException(
                    "All API controller actions must declare an explicit named authorization policy. Missing policy on: " +
                    string.Join(", ", violations));
            }

            return app;
        }

        private static bool HasNamedPolicy(ControllerActionDescriptor action)
        {
            var authorizeData = GetAuthorizeData(action).ToList();
            return authorizeData.Any(data => !string.IsNullOrWhiteSpace(data.Policy));
        }

        private static bool HasAllowAnonymous(ControllerActionDescriptor action)
        {
            if (action.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any())
                return true;

            return EnumerateTypeHierarchy(action.ControllerTypeInfo)
                .SelectMany(type => type.GetCustomAttributes(inherit: false).OfType<IAllowAnonymous>())
                .Any();
        }

        private static IEnumerable<IAuthorizeData> GetAuthorizeData(ControllerActionDescriptor action)
        {
            foreach (var attribute in action.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>())
            {
                yield return attribute;
            }

            foreach (var type in EnumerateTypeHierarchy(action.ControllerTypeInfo))
            {
                foreach (var attribute in type.GetCustomAttributes(inherit: false).OfType<IAuthorizeData>())
                {
                    yield return attribute;
                }
            }
        }

        private static IEnumerable<TypeInfo> EnumerateTypeHierarchy(TypeInfo typeInfo)
        {
            for (var current = typeInfo; current != null && current.AsType() != typeof(object); current = current.BaseType?.GetTypeInfo())
            {
                yield return current;
            }
        }
    }
}