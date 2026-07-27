using System.Globalization;
using System.Text;
using Shop.Domain.Common;
using Shop.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace Shop.Domain.Entities;

public sealed class Category : BaseEntity<Guid>, IAggregateRoot
{

     private static readonly Regex NonSlugChars = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
     private static readonly Regex Separators   = new(@"[\s-]+",       RegexOptions.Compiled);
     public string Name {get; private set;} = default!;
     public string Slug {get; private set;} = default!;
     public string? Description {get; private set;}
     public Guid? ParentId {get; private set;}

     private Category() {}

     public Category(string name, string? description = null, Guid? parentId  = null)
     {
          Id = Guid.NewGuid();
          ApplyName(name);
          Description = Normalize(description);
          ParentId = parentId;
     }
     public void Rename(string newName)
     {
          ApplyName(newName);
          MarkUpdated();
     }

     public void UpdateDescription(string? description)
     {
          Description = Normalize(description);
          MarkUpdated();
     }

     public void MoveTo(Guid? newParentId)
     {
          if(newParentId == Id)
               throw new InvalidCategoryException("Danh mục không thể là cha của chính nó");
          ParentId = newParentId;
          MarkUpdated();
     }

     private void ApplyName(string name)
     {
          if(string.IsNullOrWhiteSpace(name))
               throw new InvalidCategoryException("Tên danh mục không thể để trống");
          name = name.Trim();
          if(name.Length > 200)
               throw new InvalidCategoryException("Tên danh mục không thể vượt quá 200 ký tự");
          Name = name;
          Slug = GenerateSlug(name);
     }
     private static string? Normalize(string? value)
          => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
     
     private static string GenerateSlug(string name)
     {
          var lowered = name.ToLowerInvariant().Replace('đ','d');
          var decomposed = lowered.Normalize(NormalizationForm.FormD);
          var sb = new StringBuilder(decomposed.Length);
          foreach(var c in decomposed)
          {
               if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
          }
          var slug = sb.ToString().Normalize(NormalizationForm.FormC);
          slug = NonSlugChars.Replace(slug, string.Empty);
          slug = Separators.Replace(slug, "-");
          slug = slug.Trim('-');

          if(slug.Length == 0)
               throw new InvalidCategoryException($"Tên danh mục '{name}' không tạo được slug hợp lệ");

          return slug;
     }
}