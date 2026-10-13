using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Tests.PlayMode.S3
{
    /// <summary>InputRouter whose events tests raise by hand, instead of reading real devices.</summary>
    public sealed class FakeInputRouter : InputRouter
    {
        public void SendMove(Vector2 value) => RaiseMove(value);
        public void SendSprint(bool value) => RaiseSprintChanged(value);
    }
}
