using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace BlazorApp1.Templating
{
internal class VirtualRazorCodeDocument : RazorCodeDocument
{
   public override IReadOnlyList<RazorSourceDocument> Imports => Array.Empty<RazorSourceDocument>();
   public override ItemCollection Items { get; }
   public override RazorSourceDocument Source { get; }

   public VirtualRazorCodeDocument(string template)
   {
      Items = new ItemCollection();
      Source = RazorSourceDocument.Create(template, "DynamicTemplate.cshtml");
   }
}
}
