using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodLineAPI.Domain.Enums
{
    public enum BloodBagStatus
    {
        Available = 0,
        Expired = 3,
        Discarded = 4,
        Testing = 5,
        Exported = 6
    }
}
