using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodBankSystem.Domain.Enums
{
    public enum BloodBagStatus
    {
        Available,
        Reserved,
        Used,
        Expired,
        Discarded,
        Testing
    }
}
