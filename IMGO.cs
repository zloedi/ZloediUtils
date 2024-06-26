using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;

using UnityEngine;


public class ImObject {
    public float garbageAge;
    public bool garbageMaterials;
    public Renderer [] rends;
    public Material [] mats;
    public GameObject go;
    // matches the lookupChildren array size
    public List<Transform> [] refChildren = new List<Transform>[0];

    public bool GetRefs( int iList, out List<Transform> list ) {
        if ( refChildren.Length == 0 ) {
            list = null;
            return false;
        }
        list = refChildren[iList];
        return true;
    }

    public Transform GetRef( int iList, int iElem ) {
        if ( GetRefs( iList, out List<Transform> list ) ) {
            if ( list.Count == 0 ) {
                return null;
            }
            return list[iElem];
        }
        return null;
    }
}


public static class IMGO {


public static Transform root;

static Dictionary<int,ImObject> _cache = new Dictionary<int,ImObject>();
static HashSet<ImObject> _garbage = new HashSet<ImObject>();
static List<ImObject> _dead = new List<ImObject>();
static List<ImObject> _tickItems = new List<ImObject>();
static Dictionary<string,Texture2D> _stringTextures = new Dictionary<string,Texture2D>();
static GameObject _root;
static GameObject _sprite;

public static void Begin() {
    // the items from the previous tick are potentially garbage
    foreach ( var i in _tickItems ) {
        _garbage.Add( i );
    }
    _tickItems.Clear();
}

public static void End() {
    foreach ( var i in _tickItems ) {
        _garbage.Remove( i );
    }

    _dead.Clear();

    foreach ( var i in _garbage ) {
        i.garbageAge += Time.deltaTime;

        if ( ! i.go ) {
            _dead.Add( i );
            continue;
        }

        if ( i.garbageAge > 5f ) {

            if ( i.garbageMaterials ) {
                foreach ( var m in i.mats ) {
                    Destroy( m );
                }
            }

            foreach ( var r in i.rends ) {
                foreach ( var kv in _stringTextures ) {
                    if ( kv.Value == r.material.mainTexture ) {
                        _stringTextures.Remove( kv.Key );
                        Destroy( r.material.mainTexture );
                        break;
                    }
                }
            }

            DestroyGO( i.go );

            //Qonsole.Log( $"Removing IMM garbage {i.go.name}" );
            _dead.Add( i );
        } else {
            i.go.SetActive( false );
        }
    }

    foreach ( var i in _dead ) {
        _garbage.Remove( i );
    }

    for ( int i = 0; i < _tickItems.Count; i++ ) {
        var imti = _tickItems[i];
        imti.go.SetActive( true );
        imti.go.transform.SetSiblingIndex( i );
    }
}

public static int NextHashWg( int hash, int val ) {
    return hash * 31 + val + 1;
}

public static int HashWg( int lineNumber, string caller ) {
    int id = 23;
    id = NextHashWg( id, lineNumber + 1 );
    id = NextHashWg( id, caller.GetHashCode() );
    return id;
}

public static ImObject RegisterPrefab( GameObject prefab, Action<GameObject> onCreate = null,
                                                        int layer = -1,
                                                        bool garbageMaterials = true,
                                                        string [] lookupChildren = null,
                                                        int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    ImObject imo;
    if ( ! _cache.TryGetValue( handle, out imo ) || ! imo.go ) {
        GameObject go = GameObject.Instantiate( prefab );

        imo = new ImObject {
            go = go,
            garbageMaterials = garbageMaterials,
            rends = go.GetComponentsInChildren<Renderer>( includeInactive: true ),
        };

        if ( imo.rends != null && imo.rends.Length > 0 ) {
            imo.mats = new Material[imo.rends.Length];
            for ( int i = 0; i < imo.rends.Length; i++ ) {
                if ( garbageMaterials ) {
                    imo.mats[i] = imo.rends[i].material;
                } else {
                    imo.mats[i] = imo.rends[i].sharedMaterial;
                }
            }
        } else {
            imo.mats = new Material[0];
        }

        if ( lookupChildren != null ) {
            imo.refChildren = new List<Transform>[lookupChildren.Length];
            for ( int i = 0; i < imo.refChildren.Length; i++ ) {
                imo.refChildren[i] = new List<Transform>();
            }
            Transform [] ts = imo.go.GetComponentsInChildren<Transform>();
            foreach ( Transform t in ts ) {
                string ln = t.name.ToLowerInvariant();
                int roll = 0; 
                for ( int i = 0; i < lookupChildren.Length; i++ ) {
                    int idx = ( i + roll ) % lookupChildren.Length;
                    if ( ln.Contains( lookupChildren[idx].ToLowerInvariant() ) ) {
                        imo.refChildren[idx].Add( t );
                        roll++;
                        break;
                    }
                }
            }
        }

        _cache[handle] = imo;
        if ( ! _root ) {
            _root = new GameObject( "imm_root" );
            _root.transform.parent = root;
        }
        go.transform.parent = _root.transform;
        go.name = $"{prefab.name}{handle.ToString( "X8" )}";
        if ( layer != -1 ) {
            go.layer = layer;
        }
        if ( onCreate != null ) {
            onCreate( go );
        }
        //Qonsole.Log( "Created game object on IMM draw." );
    }
    _tickItems.Add( imo );
    return imo;
}

public static GameObject WorldText( string s, Vector3 pos, float scale = 1,
                                                        int layer = -1,
                                                        Color? color = null,
                                                        Vector4? border = null,
                                                        int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( s.GetHashCode(), handle );
    Texture2D tex;
    if ( ! _stringTextures.TryGetValue( s, out tex ) ) {
        _stringTextures[s] = tex = AppleFont.CreateStringTexture( s );
    }
    GameObject go = SpriteTex( tex, pos, null, scale, layer, color, border, handle, lineNumber,
                                                                                        caller );
    if ( Camera.main ) {
        go.transform.forward = Camera.main.transform.forward;
    }
    return go;
}

public static GameObject SpriteTex( Texture2D tex, Vector3 pos, Material mat = null, float scale = 1,
                                                        int layer = -1,
                                                        Color? color = null,
                                                        Vector4? border = null,
                                                        int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    if ( ! _sprite ) {
        _sprite = new GameObject( "IMMSpritePrefab" );
        _sprite.AddComponent<SpriteRenderer>();
        _sprite.transform.parent = root;
        _sprite.SetActive( false );
    }
    ImObject imo = RegisterPrefab( _sprite, layer: layer, garbageMaterials: false,
                                                                                handle: handle );
    imo.go.transform.position = pos;
    imo.go.transform.localScale = Vector3.one * scale;
    foreach ( var r in imo.rends ) {
        var sr = ( SpriteRenderer )r;
        if ( ! sr.sprite || sr.sprite.texture != tex ) {
            Destroy( sr.sprite );
            sr.sprite = UnityEngine.Sprite.Create( tex, new Rect( 0.0f, 0.0f, tex.width, tex.height ),
                                                    Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect,
                                                    border != null ? border.Value : Vector4.zero );
        }
        sr.color = color != null ? color.Value : Color.white;
    }
    return imo.go;
}

static void DestroyGO( GameObject go ) {
    if ( Application.isPlaying ) {
        GameObject.Destroy( go );
    } else {
        GameObject.DestroyImmediate( go );
    }
}

static void Destroy( UnityEngine.Object o ) {
    if ( Application.isPlaying ) {
        UnityEngine.Object.Destroy( o );
    } else {
        UnityEngine.Object.DestroyImmediate( o );
    }
}


}
