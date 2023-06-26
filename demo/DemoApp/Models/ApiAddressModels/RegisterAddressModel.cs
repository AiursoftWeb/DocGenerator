using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DemoApiApp.Sdk.Models.ApiAddressModels;

public class RegisterAddressModel
{
    [FromForm(Name = "user-name")]
    [JsonProperty(PropertyName = "uname")]
    [Required]
    [MaxLength(10)]
    public string? Name { get; set; }
    [Required]
    public string? Password { get; set; }
}