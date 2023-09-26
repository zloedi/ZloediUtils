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
public static bool ShowStats_cvar = false;
public static bool SkipDrawMeshes_cvar = false;
public static bool SkipCollisionTests_cvar = false;

public static Material XRayPassObstruct;
public static Material XRayPassActorFriendly;
public static Material XRayPassActorHostile;

static Vector3 _eye => Camera.main ? Camera.main.transform.position : Vector3.zero;
static HashSet<Renderer> _rendsWithXRay = new HashSet<Renderer>();
static HashSet<Component> _actorsWithXRay = new HashSet<Component>();
static HashSet<Renderer> _seethroughObjects = new HashSet<Renderer>();
static Dictionary<Component,bool> _hostile = new Dictionary<Component,bool>();
static int _numDrawnMeshes = 0;

public static void DrawMeshes( Camera camera, Renderer rend, Material material, 
                                                    MaterialPropertyBlock [] mpb, int layer = -1 ) {
    MeshFilter [] filters = rend.GetComponents<MeshFilter>();

    if ( layer == -1 ) {
        layer = rend.gameObject.layer;
    }

    foreach ( var f in filters ) {
        Mesh mesh = f.sharedMesh;
        if ( mesh ) {
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
                _numDrawnMeshes++;
            }
        } else {
#if UNITY_EDITOR
            Debug.LogWarning("ObstructFader: mesh filter is missing a mesh on '" + rend.gameObject + "'", rend.gameObject);
#endif
        }
    } 
}


static Material GetXRayActorMaterial( bool hostile ) {
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

static bool CanHaveXRayMaterial(Renderer r) {
    return r.shadowCastingMode != ShadowCastingMode.Off 
            && (r is MeshRenderer || r is SkinnedMeshRenderer);
            // this check is redundant, since doesn't cast shadow
            //&& r.sharedMaterial != XRayPassActor);
}

static bool IsXRayProxy(Renderer r) {
    foreach ( var m in r.sharedMaterials ) {
        if ( m == XRayPassActorFriendly || m == XRayPassActorHostile ) {
            return true;
        }
    }
    return false;
}

static bool ShouldSetupXRayFor( Renderer renderer ) {
    if ( _rendsWithXRay.Contains( renderer ) ) {
        return false;
    }
    if ( ! CanHaveXRayMaterial(renderer)) {
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

static bool ShouldSetupXRayFor( Component actor ) {
    return ! _actorsWithXRay.Contains( actor );
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

    _seethroughObjects.Clear();
    int totalNumColliders = 0;
    Collider [] colBuffer = new Collider[256];

    for ( int iactor = 0; iactor < actors.Count; iactor++ ) {
        Component a = actors[iactor];
        // FIXME: should remove older mats if changes factions
        // FIXME: ShouldSetupXRayFor is broken, and actors should
        // be filtered on camera clip tests/mask-draw 
        Material xrayMat = GetXRayActorMaterial( isHostile[iactor] );
        if ( ShouldSetupXRayFor( a ) ) {
            _hostile[a] = isHostile[iactor];
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
            _actorsWithXRay.Add( a );
        }

        if ( ! _hostile.TryGetValue( a, out bool hostile ) || hostile != isHostile[iactor] ) {
            _hostile[a] = isHostile[iactor];
            Renderer [] rs = a.GetComponentsInChildren<Renderer>();
            foreach ( var r in rs ) {
                if ( ! IsXRayProxy( r ) ) {
                    continue;
                }
                Log( $"Rebinding materials on {r} to {xrayMat}..." );
#if true
                Material [] mats = new Material[r.sharedMaterials.Length];
                r.sharedMaterials = new Material[0];
                for ( int j = 0; j < mats.Length; j++ ) {
                    mats[j] = xrayMat;
                }
                r.sharedMaterials = mats;
#else
                for ( int i = 0; i < r.sharedMaterials.Length; i++ ) {
                    r.sharedMaterials[i] = xrayMat;
                }
#endif
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
            int numColliders = 0;
            if ( ! SkipCollisionTests_cvar ) {
                numColliders = Physics.OverlapCapsuleNonAlloc( _eye, lookat, radius, colBuffer,
                                                                                            -1 );
            }

            if ( numColliders > 0 ) {
                //DebugEx.DrawSphere(_eye, radius);
                Debug.DrawLine(_eye, lookat, Color.green, duration: 0);
                //DebugEx.DrawSphere(lookat, radius);
            } else {
                //DebugEx.DrawSphere(_eye, radius);
                Debug.DrawLine(_eye, lookat, Color.white, duration: 0);
                //DebugEx.DrawSphere(lookat, radius);
            }

            for ( int i = 0; i < numColliders; i++ ) {
                Renderer r = GetValidObstructRenderer( colBuffer[i] );
                if ( r ) {
                    _seethroughObjects.Add( r );
                }
            }

            totalNumColliders += numColliders;
        }
    }

    if ( Camera.main && ! SkipDrawMeshes_cvar ) {
        // draw the obstructions as mask in the stencil
        _numDrawnMeshes = 0;
        foreach ( var r in _seethroughObjects ) {
            DrawMeshes( Camera.main, r, XRayPassObstruct, null );
        }
    }

    if ( ShowStats_cvar ) {
        Log( $"Num seethrough objects: {_seethroughObjects.Count}" );
        Log( $"Num colliders hit: {totalNumColliders}" );
        Log( $"Num drawn meshes: {_numDrawnMeshes}" );
        Log( "\n" );
    }
}


}
