﻿using nobnak.Gist;
using UnityEngine;

public class VoxelCameraDirection {
    public enum DirectionEnum { LookForward = 0, LookRight = 1, LookUp = 2 }

    public static readonly Vector3 NORMALIZED_ORIGIN = 0.5f * Vector3.one;
    public static readonly Vector3 NORMALIZED_RIGHT = 0.5f * new Vector3 (1f, 0f, 0f);
    public static readonly Vector3 NORMALIZED_UP = 0.5f * new Vector3 (0f, 1f, 0f);
    public static readonly Vector3 NORMALIZED_FORWARD = 0.5f * new Vector3 (0f, 0f, 1f);

    public readonly float cameraToVoxelDistance;

    DirectionEnum direction;
    Matrix4x4 basisRotationMatrix;

    public VoxelCameraDirection() : this(1f, DirectionEnum.LookForward) {
    }
    public VoxelCameraDirection(float cameraToVoxelDistance, DirectionEnum direction) {
        this.cameraToVoxelDistance = cameraToVoxelDistance;
        ChangeDirection (direction);
    }

    public DirectionEnum Direction {
        get { return direction; }
        set { 
            if (direction != value)
                ChangeDirection (value); 
        }
    }
    public Matrix4x4 VoxelDirection {
        get { return basisRotationMatrix; }
    }

    public void FitCameraToVoxelBounds (Camera cam, AbstractVoxelBounds voxelBounds) {
        var origin = voxelBounds.NormalizedToWorldPosition (basisRotationMatrix.MultiplyVector (NORMALIZED_ORIGIN));
        var right = voxelBounds.NormalizedToWorldPosition (basisRotationMatrix.MultiplyVector (NORMALIZED_RIGHT) + NORMALIZED_ORIGIN) - origin;
        var up = voxelBounds.NormalizedToWorldPosition (basisRotationMatrix.MultiplyVector (NORMALIZED_UP) + NORMALIZED_ORIGIN) - origin;
        var forward = voxelBounds.NormalizedToWorldPosition (basisRotationMatrix.MultiplyVector (NORMALIZED_FORWARD) + NORMALIZED_ORIGIN) - origin;

        var upLength = up.magnitude;
        var rightLength = right.magnitude;
        var forwardLength = forward.magnitude;

        var nearClipPlane = cameraToVoxelDistance;
        var farClipPlane = nearClipPlane + 2f * forwardLength;

        cam.transform.position = origin - (nearClipPlane / forwardLength + 1f) * forward;
        cam.transform.rotation = Quaternion.LookRotation (forward, up);
        cam.orthographic = true;
        cam.orthographicSize = upLength;
        cam.nearClipPlane = nearClipPlane;
        cam.farClipPlane = farClipPlane;
        cam.aspect = rightLength / upLength;
    }

    Matrix4x4 ShiftRight(Matrix4x4 m) {
        var n = Matrix4x4.identity;
        for (var i = 0; i < 3; i++)
            n.SetColumn (i, m.GetColumn ((i + 1) % 3));
        return n;
    }
    Matrix4x4 ShiftRight(Matrix4x4 m, int count) {
        for (var i = 0; i < count; i++)
            m = ShiftRight (m);
        return m;
    }

    void ChangeDirection (DirectionEnum direction) {
        this.direction = direction;
        basisRotationMatrix = ShiftRight (Matrix4x4.identity, (int)direction);
    }
}
