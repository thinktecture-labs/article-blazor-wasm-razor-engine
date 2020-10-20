using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorApp1.Templating
{
   public class Template<TModel>
      where TModel : class
   {
      private readonly IServiceProvider _serviceProvider;
      private readonly Type _viewType;

      public Template(
         IServiceProvider serviceProvider,
         Type viewType)
      {
         _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
         _viewType = viewType ?? throw new ArgumentNullException(nameof(viewType));
      }

      public async Task<string> GetRenderedTextAsync(TModel model)
      {
         var razorView = (TemplateBase<TModel>)ActivatorUtilities.CreateInstance(_serviceProvider, _viewType);

         if (razorView == null)
            throw new Exception("Could not create an instance of previously compiled razor view.");

         razorView.Model = model;
         await razorView.ExecuteAsync();

         return razorView.GetRenderedText();
      }
   }
}
