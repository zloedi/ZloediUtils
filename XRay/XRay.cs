using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// make sure the stencil is not overriden in any URP render assets

public static class XRay {


public static Action<string> Log = s => Debug.Log( s );
public static Action<string> Error = s => Debug.LogError( s );

public static bool Enabled_cvar = false;

public static Material XRayPassObstruct;
public static Material XRayPassActorFriendly;
public static Material XRayPassActorHostile;

static Vector3 _eye => Camera.main ? Camera.main.transform.position : Vector3.zero;

public static void DrawMeshes( Camera camera, Renderer rend, Material material, 
                                                    MaterialPropertyBlock [] mpb, int layer = -1 ) {
    MeshFilter [] filters = rend.GetComponents<MeshFilter>();
    if ( filters.Length == 0 ) {
        Qonsole.Log( "no filters." );
    }

    if ( layer == -1 ) {
        layer = rend.gameObject.layer;
    }
    foreach ( var f in filters ) {
        Mesh mesh = f.sharedMesh;
        if ( mesh ) {
            //Qonsole.Log( f.gameObject.name + ": " + mesh.subMeshCount );
            for ( int i = 0; i < mesh.subMeshCount; i++ ) {
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


static Material GetXRayActorMaterial( Component a, bool hostile ) {
    return hostile ? XRayPassActorHostile : XRayPassActorFriendly;
}

// FIXME: no such thing in unity?
static bool IsVisibleFromCamera( GameObject go ) {
    Renderer [] rs = go.GetComponentsInChildren<Renderer>();
    foreach ( var r in rs ) {
        return r.enabled && r.isVisible && ! IsXRayProxy(r);
    }
    return false;
}

static bool CanHaveXRayMaterial(Renderer r) 
{
    return r.shadowCastingMode != ShadowCastingMode.Off 
            && (r is MeshRenderer || r is SkinnedMeshRenderer);
            // this check is redundant, since doesn't cast shadow
            //&& r.sharedMaterial != XRayPassActor);
}

static HashSet<Renderer> _rendsWithXRay = new HashSet<Renderer>();

static bool IsXRayProxy(Renderer r) {
    foreach ( var m in r.sharedMaterials ) {
        if ( m == XRayPassActorFriendly || m == XRayPassActorHostile ) {
            return true;
        }
    }
    return false;
}

static bool ShouldSetupXRayFor(Renderer renderer)
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

static bool ShouldSetupXRayFor( bool actor ) {
    return true;
}

static Renderer GetValidObstructRenderer( Collider c ) {
    var rend = c.GetComponent<Renderer>();
    if ( CanHaveObstructMaterial( rend ) ) {
        return rend;
    }
    return null;
}

public static bool CanHaveObstructMaterial( Renderer rend ) {
    if ( ! rend ) {
        return false;
    }

    if ( ! ( rend is MeshRenderer || rend is SkinnedMeshRenderer ) ) {
        return false;
    }

    if ( _rendsWithXRay.Contains( rend ) ) {
        return false;
    }

    if ( rend is SkinnedMeshRenderer ) {
        return false;
    }

    return true;
}

public static void Tick( IList<Component> actors, IList<bool> isHostile ) {
    if ( ! Enabled_cvar ) {
        return;
    }

    if ( actors == null || actors.Count == 0 ) {
        return;
    }

    if ( ! XRayPassObstruct || ! XRayPassActorFriendly || ! XRayPassActorHostile ) {
        Error( "Can't find materials." );
        return;
    }

    Collider [] colBuffer = new Collider[256];
    for ( int iactor = 0; iactor < actors.Count; iactor++ ) {
        var a = actors[iactor];
        // FIXME: should remove older mats if changes factions
        // FIXME: ShouldSetupXRayFor is broken, and actors should
        // be filtered on camera clip tests/mask-draw 
        var xrayMat = GetXRayActorMaterial( a, isHostile[iactor] );
        if ( ShouldSetupXRayFor( a ) ) {
            Renderer [] rs = a.GetComponentsInChildren<Renderer>();
            foreach ( var r in rs ) {
                if ( ShouldSetupXRayFor( r ) ) {
                    Log( "Setting up XRay shader on " + r );
                    var clone = GameObject.Instantiate( r.gameObject );
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
                    foreach ( var c in comps ) {
                        if (! ( c is Transform || c is Renderer || c is MeshFilter ) ) {
                            UnityEngine.Object.Destroy(c);
                        }
                    }
                    _rendsWithXRay.Add(r);
                }
            }
        }

        if ( IsVisibleFromCamera( a.gameObject ) ) {
            // FIXME: trigger tests only when the camera moves
            //var d = a.transform.position - _camera.CenterWorldPos;
            //if (d.sqrMagnitude > 0.001f) {

            float radius = 2.4f / 2;
            Vector3 lookat = a.transform.position + new Vector3(0, radius, 0);
            Vector3 v = (lookat - _eye).normalized;
            lookat -= v * radius;
            int numColliders = Physics.OverlapCapsuleNonAlloc( _eye, lookat, radius, colBuffer,
                                                                                            -1 );
            if ( numColliders > 0 ) {
                //DebugEx.DrawSphere(_eye, radius);
                Debug.DrawLine(_eye, lookat, Color.green, duration: 0);
                //DebugEx.DrawSphere(lookat, radius);
            } else {
                //DebugEx.DrawSphere(_eye, radius);
                Debug.DrawLine(_eye, lookat, Color.white, duration: 0);
                //DebugEx.DrawSphere(lookat, radius);
            }

            // draw the obstructions as mask in the stencil
            for ( int i = 0; i < numColliders; i++ ) {
                Renderer r = GetValidObstructRenderer( colBuffer[i] );
                if ( r && Camera.main ) {
                    DrawMeshes( Camera.main, r, XRayPassObstruct, null );
                }
            }
        }
    }
}


}
