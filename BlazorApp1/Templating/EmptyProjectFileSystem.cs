using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace BlazorApp1.Templating
{
   internal class EmptyProjectFileSystem : RazorProjectFileSystem
   {
      public static readonly EmptyProjectFileSystem Instance = new EmptyProjectFileSystem();

      public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
      {
         return Enumerable.Empty<RazorProjectItem>();
      }

      public override RazorProjectItem GetItem(string path)
      {
         return GetItem(path, String.Empty);
      }

      public override RazorProjectItem GetItem(string path, string fileKind)
      {
         return new NotFoundProjectItem(String.Empty, path);
      }

      private class NotFoundProjectItem : RazorProjectItem
      {
         public override string BasePath { get; }
         public override string FilePath { get; }
         public override bool Exists => false;
         public override string PhysicalPath => throw new NotSupportedException();
         public override Stream Read() => throw new NotSupportedException();

         public NotFoundProjectItem(string basePath, string path)
         {
            BasePath = basePath;
            FilePath = path;
         }
      }
   }
}
