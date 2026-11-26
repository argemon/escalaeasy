using System.Text.Json;
using EscalaEasy.Application.Contracts;
using EscalaEasy.Domain.Scheduling;
namespace EscalaEasy.Application.Validation;
public static class PublicationSafety {
 public static string? Error(EscalaEasyDocument d){var all=d.Publications??[];var selections=d.PublicationSelections??[];var options=new JsonSerializerOptions(JsonSerializerDefaults.Web){Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull};
 if(all.Count>10||selections.Count>10||all.Any(v=>!PublicationPolicy.Valid(v)||!ScheduleJson.ValidTimeZone(v.CompanyTimeZone))||all.Select(v=>v.Id).Distinct().Count()!=all.Count||all.Where((v,i)=>v.Number!=i+1).Any()||all.Any(v=>JsonSerializer.SerializeToUtf8Bytes(v,options).Length>262144)||JsonSerializer.SerializeToUtf8Bytes(all,options).Length>524288)return "Versões fechadas inválidas/acima da cota. Arquive/exporte o documento.";
 if(selections.Any(s=>s is null||!all.Any(v=>v.Id==s.VersionId&&v.Period.Start==s.Start&&v.Period.End==s.End))||selections.Select(s=>s.Start+"/"+s.End).Distinct().Count()!=selections.Count)return "Versão vigente inválida.";return null;
 }
}
