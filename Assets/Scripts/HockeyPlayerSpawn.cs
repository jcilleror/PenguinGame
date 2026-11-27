using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class HockeyPlayerSpawn : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner && HockeyScoreManager.Instance != null)
        {
            TeleportToAssignedSpawn();
        }
    }

    public void TeleportToAssignedSpawn()
    {
        if (HockeyScoreManager.Instance == null) return;

        Transform spawnPoint = HockeyScoreManager.Instance.GetSpawnPointForClient(OwnerClientId);
        if (spawnPoint != null)
        {
            // Team A has even ClientId (0, 2...), Team B has odd ClientId (1, 3...)
            bool isTeamA = (OwnerClientId % 2 == 0);

            // Flip 180 degrees on Y ONLY for Team A
            Quaternion targetRotation = isTeamA
                ? spawnPoint.rotation * Quaternion.Euler(0f, 180f, 0f)
                : spawnPoint.rotation;

            TeleportTo(spawnPoint.position, targetRotation);
        }
    }

    [ClientRpc]
    public void ResetToSpawnClientRpc()
    {
        if (!IsOwner) return;
        TeleportToAssignedSpawn();
    }

    private void TeleportTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(targetPosition, targetRotation, transform.localScale);
        }
        else
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        if (cc != null) cc.enabled = true;
    }
}