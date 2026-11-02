using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class MfaRecoveryCode
{
    public Guid Id { get; set; }

    public Guid MfaRecoveryCodeSetId { get; set; }

    public string CodeHash { get; set; } = null!;

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MfaRecoveryCodeSet MfaRecoveryCodeSet { get; set; } = null!;
}
