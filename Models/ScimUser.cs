using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class ScimUser
{
    public Guid Id { get; set; }

    public Guid SsoProviderId { get; set; }

    public Guid? UserId { get; set; }

    public string Resource { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string? ExternalId { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual SsoProvider SsoProvider { get; set; } = null!;

    public virtual User? User { get; set; }
}
