using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.Models;

/// <summary>
///    A transaction sequence of transformations (crop, rotate, etc)
/// </summary>
public class Transformations
{
    [Key] public int TransformationId { get; set; }

    [Required] public virtual Image Image { get; set; }

    public int ImageId { get; set; }

    public string TransformsJson { get; set; }
}
