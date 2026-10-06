using UnityEngine;

namespace Brotherhood
{
    // The source villagers animate their sprites, not their world transforms.
    // Keep them out of camera/parallax displacement even when their scenery moves.
    public sealed class SourceWorldAnchor : MonoBehaviour
    {
        Vector3 worldPosition;
        void Start() { worldPosition = transform.position; }
        void LateUpdate() { transform.position = worldPosition; }
    }
}
