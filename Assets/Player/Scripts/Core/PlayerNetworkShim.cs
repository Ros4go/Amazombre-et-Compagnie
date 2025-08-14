// Amazombre: Express Delivery - Multiplayer-ready shim
// Drop-in placeholder so you can swap to Mirror or NGO later without changing gameplay code.
using UnityEngine;

namespace Amazombre.Player.Core
{
    public class PlayerNetworkShim : MonoBehaviour
    {
        // In Mirror, replace with "public bool isLocalPlayer"
        // In NGO, replace with "public bool IsOwner"
        [SerializeField] private bool simulateLocalAuthority = true;

        public bool HasInputAuthority => simulateLocalAuthority;

        // Called by networking layer when this player becomes local
        public void SetLocalAuthority(bool isLocal)
        {
            simulateLocalAuthority = isLocal;
        }
    }
}
