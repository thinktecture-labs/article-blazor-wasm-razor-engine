using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BlazorApp1.Templating
{
   public class TemplateEngine
   {
      private const string _DLL_NAME = "TemplatingEngine.DynamicCodeCompilation";

      private readonly IServiceProvider _serviceProvider;
      private readonly HttpClient _httpClient;

      public TemplateEngine(
         IServiceProvider serviceProvider,
         HttpClient httpClient)
      {
         _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
      }

      public async Task<Template<TModel>> CompileAsync<TModel>(
         string template,
         TModel? model = null)
         where TModel : class
      {
         if (String.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Template cannot be empty.", nameof(template));

         var modelRuntimeType = model?.GetType();
         var razorCodeDoc = GenerateView<TModel>(template, modelRuntimeType);
         var assembly = await CompileViewAsync<TModel>(razorCodeDoc, modelRuntimeType);

         var razorViewType = assembly.GetTypes().FirstOrDefault(t => t.Name == "Template");

         if (razorViewType == null)
            throw new Exception("Previously compiled razor view not found");

         return new Template<TModel>(_serviceProvider, razorViewType);
      }

      private static RazorCodeDocument GenerateView<TModel>(
         string template,
         Type? modelRuntimeType)
         where TModel : class
      {
         var modelTypeName = GetModelTypeName<TModel>();
         template = $@"@inherits {typeof(TemplateBase<>).Namespace}.TemplateBase<{modelTypeName}>
{template}";

         var engine = RazorProjectEngine.Create(RazorConfiguration.Default, EmptyProjectFileSystem.Instance,
                                                builder =>
                                                {
                                                   builder.SetNamespace("System");
                                                   builder.SetNamespace("System.Array");
                                                   builder.SetNamespace("System.Collections");
                                                   builder.SetNamespace("System.Collections.Generics");

                                                   var modelNamespace = modelRuntimeType?.Namespace;

                                                   if (modelNamespace != null)
                                                      builder.SetNamespace(modelNamespace);
                                                });

         var doc = new VirtualRazorCodeDocument(template);
         engine.Engine.Process(doc);

         return doc;
      }

      private async Task<Assembly> CompileViewAsync<TModel>(
         RazorCodeDocument razorCodeDoc,
         Type? modelRuntimeType)
         where TModel : class
      {
         var csharpDoc = razorCodeDoc.GetCSharpDocument();
         var tree = CSharpSyntaxTree.ParseText(csharpDoc.GeneratedCode);
         var assemblyLocations = new HashSet<string>
                                 {
                                    "mscorlib.dll",
                                    "netstandard.dll",
                                    Path.GetFileName(Assembly.GetExecutingAssembly().Location), // add current DLL
                                    typeof(TModel).Assembly.Location                            // add model DLL
                                 };

         if (modelRuntimeType != null)
            assemblyLocations.Add(modelRuntimeType.Assembly.Location);

         var compilation = CSharpCompilation.Create(_DLL_NAME, new[] { tree },
                                                    await GetMetadataReferencesAsync(assemblyLocations),
                                                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

         await using var assemblyStream = new MemoryStream();

         var result = compilation.Emit(assemblyStream);

         if (!result.Success)
            throw new Exception($"Could not compile the provided template. Errors:{Environment.NewLine}{String.Join(Environment.NewLine, result.Diagnostics)}");

         return Assembly.Load(assemblyStream.ToArray());
      }

      private async Task<IReadOnlyList<MetadataReference>> GetMetadataReferencesAsync(
         IEnumerable<string> assemblyLocations)
      {
         var tasks = assemblyLocations.Select(GetMetadataReferenceAsync);

         return await Task.WhenAll(tasks);
      }

      private async Task<MetadataReference> GetMetadataReferenceAsync(string name)
      {
         var responseMessage = await _httpClient.GetAsync($"_framework/_bin/{WebUtility.UrlEncode(name)}");
         responseMessage.EnsureSuccessStatusCode();

         return MetadataReference.CreateFromStream(await responseMessage.Content.ReadAsStreamAsync());
      }

      private static string GetModelTypeName<TModel>()
         where TModel : class
      {
         var type = typeof(TModel);

         if (type.IsGenericType)
            throw new NotSupportedException($"Generic models are not supported. Model: {type.Name}");

         var name = type.FullName ?? throw new Exception($"The full name of the model type is empty. Type: '{type}'.");

         name = name.Replace("+", "."); // for nested types

         return name;
      }
   }
}
