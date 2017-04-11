﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FrustumVoxelizer : MonoBehaviour {
	public const int COMPUTE_CLEAR_THREADS = 8;

	public const string RESULT = "Result";
	public const string VOXEL_SIZE = "_VoxelSize";
	public const string VOXEL_TEX = "_VoxelTex";
	int PROP_RESULT;
	int PROP_VOXEL_SIZE;
	int PROP_VOXEL_TEX;

	public int prefferedVoxelResolution = 512;
	public FilterMode voxelFilterMode = FilterMode.Bilinear;

	public ComputeShader clearCompute;
	public Shader voxelShader;
	public Material visualMat;

	int currentResolusion;
	Camera targetCam;
	RenderTexture voxelTex;

	#region Unity
	void Awake () {
		PROP_RESULT = Shader.PropertyToID (RESULT);
		PROP_VOXEL_SIZE = Shader.PropertyToID(VOXEL_SIZE);
		PROP_VOXEL_TEX = Shader.PropertyToID(VOXEL_TEX);
	}
	void OnEnable() {
		targetCam = GetComponent<Camera> ();
		targetCam.enabled = false;

		Init ();
	}
	void Update() {
		Init ();
		Clear ();
		Shader.SetGlobalVector (PROP_VOXEL_SIZE, VoxelSizeV4 ());
		Shader.SetGlobalTexture (PROP_VOXEL_TEX, voxelTex);
		Render();
	}
	void OnRenderObject() {
		var currCam = Camera.current;
		if (currCam == targetCam || (currCam.cullingMask & (1 << gameObject.layer)) == 0)
			return;

		visualMat.SetPass (0);
		Graphics.DrawProcedural (MeshTopology.Triangles, 6);
	}
	void OnDisable() {
		Release (ref voxelTex);
		ReleaseCameraTargetTexture ();
	}
	#endregion

	void Init () {
		currentResolusion = Mathf.Max (COMPUTE_CLEAR_THREADS, prefferedVoxelResolution).SmallestPowerOfTwoGreaterThan ();
		if (voxelTex == null || voxelTex.width != currentResolusion) {
			Debug.LogFormat ("Create Voxel Texture : Resolusion={0}", currentResolusion);
			Release (ref voxelTex);
			voxelTex = new RenderTexture (currentResolusion, currentResolusion, 0, RenderTextureFormat.ARGB32);
			voxelTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
			voxelTex.volumeDepth = currentResolusion;
			voxelTex.enableRandomWrite = true;
			voxelTex.filterMode = voxelFilterMode;
			voxelTex.wrapMode = TextureWrapMode.Clamp;
			voxelTex.Create ();

			ReleaseCameraTargetTexture ();
			targetCam.targetTexture = RenderTexture.GetTemporary (currentResolusion, currentResolusion);
		}
	}
	void Clear () {
		var g = voxelTex.width / COMPUTE_CLEAR_THREADS;
		clearCompute.SetTexture (0, PROP_RESULT, voxelTex);
		clearCompute.Dispatch (0, g, g, g);
	}
	void Render () {
		Graphics.SetRandomWriteTarget (1, voxelTex);
		targetCam.RenderWithShader (voxelShader, null);
		Graphics.ClearRandomWriteTargets ();
	}
	Vector4 VoxelSizeV4 () {
		return new Vector4 (voxelTex.width, voxelTex.height, voxelTex.volumeDepth, 0f);
	}
	void Release<T>(ref T resource) where T : Object {
		if (Application.isPlaying)
			Object.Destroy (resource);
		else
			Object.DestroyImmediate (resource);
	}

	void ReleaseCameraTargetTexture ()
	{
		var tex = targetCam.targetTexture;
		targetCam.targetTexture = null;
		RenderTexture.ReleaseTemporary (tex);
	}
}