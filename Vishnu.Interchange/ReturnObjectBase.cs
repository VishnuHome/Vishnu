using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ObjectSerializer
{
  [Serializable()]
  public abstract class ReturnObjectBase : ISerializable
  {
    public ReturnObjectBase() : base() { }

    protected ReturnObjectBase(SerializationInfo info, StreamingContext context) { }

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter = true)]
    public abstract void GetObjectData(SerializationInfo info, StreamingContext context);
  }
}
