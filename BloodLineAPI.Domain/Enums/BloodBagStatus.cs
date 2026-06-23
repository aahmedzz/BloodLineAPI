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
        Disposed = 4,
        Testing = 5,
        Issued = 6
    }
}
