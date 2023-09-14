using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class XRay {


public static bool UseXRay_cvar = false;

public Material XRayPassObstruct;
public Material XRayPassActorFriendly;
public Material XRayPassActorHostile;

static Vector3 _eye => Camera.main ? Camera.main.transform.position : Vector3.zero;

public static void DrawMeshes( Camera camera, Renderer rend, Material material, 
                                                    MaterialPropertyBlock [] mpb, int layer = -1 ) {
    MeshFilter [] filters = rend.GetComponents<MeshFilter>();
    if ( filters.Length > 0 ) {
        if ( layer == -1 ) {
            layer = rend.gameObject.layer;
        }
        foreach ( var f in filters ) {
            Mesh mesh = f.sharedMesh;
            if ( mesh ) {
                for (int i = 0; i < mesh.subMeshCount; i++) {
                    // FIXME: use DrawMeshInstanced
                    Graphics.DrawMesh(
                            mesh: mesh, 
                            matrix: rend.localToWorldMatrix,
                            material: material, 
                            layer: layer, 
                            camera: camera, 
                            submeshIndex: i,
                            properties: mpb != null ? mpb[i] : null,
                            castShadows: false,
                            receiveShadows: false,
                            useLightProbes: false);
                }
            } else {
#if UNITY_EDITOR
                Debug.LogWarning("ObstructFader: mesh filter is missing a mesh on '" + rend.gameObject + "'", rend.gameObject);
#endif
            }
        }
    } 
}


private Material GetXRayActorMaterial( Component a, bool hostile ) {
    return hostile ? XRayPassActorHostile : XRayPassActorFriendly;
}

// FIXME: no such thing in unity?
public bool IsVisibleFromCamera(GameObject go)
{
    Renderer [] rs = go.GetComponentsInChildren<Renderer>();
    foreach (var r in rs) {
        return r.enabled && r.isVisible && ! IsXRayProxy(r);
    }
    return false;
}

private bool CanHaveXRayMaterial(Renderer r) 
{
    return r.shadowCastingMode != ShadowCastingMode.Off 
            && (r is MeshRenderer || r is SkinnedMeshRenderer);
            // this check is redundant, since doesn't cast shadow
            //&& r.sharedMaterial != XRayPassActor);
}

private HashSet<Renderer> _rendsWithXRay = new HashSet<Renderer>();

private bool IsXRayProxy(Renderer r)
{
    foreach (var m in r.sharedMaterials) {
        if (m == XRayPassActorFriendly || m == XRayPassActorHostile) {
            return true;
        }
    }
    return false;
}

private bool ShouldSetupXRayFor(Renderer renderer)
{
    if (_rendsWithXRay.Contains(renderer)) {
        return false;
    }
    if (! CanHaveXRayMaterial(renderer)) {
        return false;
    }
    // look for xray material in child renderers
    foreach (Transform t in renderer.transform) {
        var r = t.GetComponent<Renderer>();
        if (r && IsXRayProxy(r)) {
            return false;
        }
    }
    return true;
}

private bool ShouldSetupXRayFor( bool actor ) {
    return true;
}


private Renderer GetValidRenderer( Collider c ) {
    return null;
}


private void UpdateActorsXRay( IList<Component> actors )
{
    Collider [] colBuffer = new Collider[256];

    foreach ( var a in actors ) {
        // FIXME: should remove older mats if changes factions
        // FIXME: ShouldSetupXRayFor is broken, and actors should
        // be filtered on camera clip tests/mask-draw 
        var xrayMat = GetXRayActorMaterial( a, false );
        if ( ShouldSetupXRayFor( a ) ) {
            Renderer [] rs = a.GetComponentsInChildren<Renderer>();
            foreach ( var r in rs ) {
                if ( ShouldSetupXRayFor( r ) ) {
                    Debug.Log("MapObstructions: Setting up XRay shader on " + r, r);
                    var clone = GameObject.Instantiate(r.gameObject);
                    clone.name = xrayMat.name;
                    clone.transform.parent = r.gameObject.transform;
                    clone.transform.localPosition = Vector3.zero;
                    clone.transform.localRotation = Quaternion.identity;
                    Renderer cloneR = clone.GetComponent<Renderer>();
                    Material [] mats = new Material[cloneR.sharedMaterials.Length];
                    for (int j = 0; j < mats.Length; j++) {
                        mats[j] = xrayMat;
                    }
                    cloneR.sharedMaterials = mats;
                    cloneR.receiveShadows = false;
                    cloneR.shadowCastingMode = ShadowCastingMode.Off;
                    cloneR.enabled = true;
                    var comps = clone.GetComponents<Component>();
                    foreach (var c in comps) {
                        if (! (c is Transform || c is Renderer || c is MeshFilter) ) {
                            Object.Destroy(c);
                        }
                    }
                    _rendsWithXRay.Add(r);
                }
            }
            //foreach (var r in rs) {
            //	if (ShouldSetupXRayFor(r)) {
            //		Debug.Log("MapObstructions: Setting up XRay shader on " + r, r);
            //		var clone = new GameObject();//GameObject.Instantiate(r.gameObject);
            //		clone.name = xrayMat.name;
            //		clone.transform.parent = r.gameObject.transform;
            //		clone.transform.localPosition = Vector3.zero;
            //		clone.transform.localRotation = Quaternion.identity;

            //		Renderer cloneR;
            //		if (r is MeshRenderer) {
            //			cloneR = clone.AddComponent<MeshRenderer>();
            //			MeshFilter mf = clone.AddComponent<MeshFilter>();
            //			mf.sharedMesh = r.gameObject.GetComponent<MeshFilter>().sharedMesh;
            //		} else {
            //			//var smr = clone.AddComponent<SkinnedMeshRenderer>();
            //			//smr.sharedMesh = (r as SkinnedMeshRenderer).sharedMesh;
            //			cloneR = Object.Instantiate(r);
            //		}

            //		Material [] mats = new Material[r.sharedMaterials.Length];
            //		for (int j = 0; j < mats.Length; j++) {
            //			mats[j] = xrayMat;
            //		}
            //		cloneR.sharedMaterials = mats;
            //		cloneR.receiveShadows = false;
            //		cloneR.shadowCastingMode = ShadowCastingMode.Off;
            //		cloneR.enabled = true;
            //		var comps = clone.GetComponents<Component>();
            //		foreach (var c in comps) {
            //			if (! (c is Transform || c is Renderer || c is MeshFilter) ) {
            //				Object.DestroyImmediate(c);
            //			}
            //		}
            //	}
            //}
        }

        if (IsVisibleFromCamera(a.gameObject)) {
            // FIXME: trigger tests only when the camera moves
            //var d = a.transform.position - _camera.CenterWorldPos;
            //if (d.sqrMagnitude > 0.001f) {

                float radius = 2.4f / 2;
                Vector3 lookat = a.transform.position + new Vector3(0, radius, 0);
                Vector3 v = (lookat - _eye).normalized;
                lookat -= v * radius;
                //DebugEx.DrawSphere(_eye, radius);
                Debug.DrawLine(_eye, lookat, Color.white, duration: 0);
                //DebugEx.DrawSphere(lookat, radius);
                int numColliders = Physics.OverlapCapsuleNonAlloc( _eye, lookat, radius, 
                                                                                    colBuffer, -1 );

                // draw the obstructions as mask in the stencil
                for ( int i = 0; i < numColliders; i++ ) {
                    Renderer r = GetValidRenderer(colBuffer[i]);
                    if ( r && Camera.main ) {
                        DrawMeshes( Camera.main, r, XRayPassObstruct, null );
                    }
                }

                //RaycastHit[] hits = GetAllToward(_eye, a.transform.position);
                //foreach (var h in hits) {
                //	ObstructFader of;
                //	Renderer r = GetValidRenderer(h.collider);
                //	if (r && GetFader(r, out of) && ! IsHidden(r.gameObject)) {
                //		ObstructFader.DrawMeshes(_camera.Camera, r, XRayPassObstruct);
                //	}
                //}
            //}
        }
    }
}


}
