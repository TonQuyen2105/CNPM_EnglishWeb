using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class MfaRecoveryCodeSet
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid MfaFactorId { get; set; }

    public int FailedVerificationCount { get; set; }

    public DateTime? VerificationLockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual MfaFactor MfaFactor { get; set; } = null!;

    public virtual ICollection<MfaRecoveryCode> MfaRecoveryCodes { get; set; } = new List<MfaRecoveryCode>();

    public virtual User User { get; set; } = null!;
}
