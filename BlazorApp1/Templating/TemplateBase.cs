using System.IO;
using System.Threading.Tasks;

namespace BlazorApp1.Templating
{
   public abstract class TemplateBase<TModel>
      where TModel : class
   {
      public TModel? Model { get; set; }

      private readonly StringWriter _writer;

      protected TemplateBase()
      {
         _writer = new StringWriter();
      }

      public string GetRenderedText()
      {
         return _writer.ToString();
      }

      public void WriteLiteral(string literal)
      {
         _writer.Write(literal);
      }

      public void Write(object obj)
      {
         _writer.Write(obj);
      }

      public virtual Task ExecuteAsync()
      {
         return Task.CompletedTask;
      }
   }
}
