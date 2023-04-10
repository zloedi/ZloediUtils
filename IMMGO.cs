using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;

using UnityEngine;


public class ImmObject {
    public int garbageAge;
    public bool garbageMaterials;
    public Renderer [] rends;
    public GameObject go;
}


public static class IMMGO {


public static Transform root;

static Dictionary<int,ImmObject> _immCache = new Dictionary<int,ImmObject>();
static HashSet<ImmObject> _immGarbage = new HashSet<ImmObject>();
static List<ImmObject> _immDead = new List<ImmObject>();
static List<ImmObject> _immTickItems = new List<ImmObject>();
static GameObject _immRoot;
static GameObject _sprite;

public static void Begin() {
    // the items from the previous tick are potentially garbage
    foreach ( var i in _immTickItems ) {
        _immGarbage.Add( i );
    }
    _immTickItems.Clear();
}

public static void End() {
    foreach ( var i in _immTickItems ) {
        _immGarbage.Remove( i );
    }

    _immDead.Clear();

    foreach ( var i in _immGarbage ) {
        i.garbageAge += Gb.time.deltaMS;

        if ( ! i.go ) {
            _immDead.Add( i );
            continue;
        }

        if ( i.garbageAge > 5000 ) {
            if ( i.garbageMaterials ) {
                foreach ( var r in i.rends ) {
                    UnityEngine.Object.Destroy( r.material );
                }
            }
            GameObject.Destroy( i.go );
            //Qonsole.Log( $"Removing IMM garbage {i.go.name}" );
            _immDead.Add( i );
        } else {
            i.go.SetActive( false );
        }
    }

    foreach ( var i in _immDead ) {
        _immGarbage.Remove( i );
    }

    for ( int i = 0; i < _immTickItems.Count; i++ ) {
        var imti = _immTickItems[i];
        imti.go.SetActive( true );
        imti.go.transform.SetSiblingIndex( i );
    }
}

public static ImmObject RegisterPrefab( GameObject prefab, Action<GameObject> onCreate = null,
                                                        int layer = -1,
                                                        bool garbageMaterials = true,
                                                        int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = QUI.NextHashWg( QUI.HashWg( lineNumber, caller ), handle );
    ImmObject imo;
    if ( ! _immCache.TryGetValue( handle, out imo ) || ! imo.go ) {
        GameObject go = GameObject.Instantiate( prefab );
        imo = new ImmObject {
            go = go,
            garbageMaterials = garbageMaterials,
            rends = go.GetComponentsInChildren<Renderer>( includeInactive: true ),
        };
        _immCache[handle] = imo;
        if ( ! _immRoot ) {
            _immRoot = new GameObject( "imm_root" );
            _immRoot.transform.parent = root;
        }
        go.transform.parent = _immRoot.transform;
        go.name = $"{prefab.name}{handle}";
        if ( layer != -1 ) {
            go.layer = layer;
        }
        if ( onCreate != null ) {
            onCreate( go );
        }
        //Qonsole.Log( "Created game object on IMM draw." );
    }
    _immTickItems.Add( imo );
    return imo;
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
    ImmObject imo = RegisterPrefab( _sprite, layer: layer, garbageMaterials: false,
                                                                                handle: handle );
    imo.go.transform.position = pos;
    imo.go.transform.localScale = Vector3.one * scale;
    foreach ( var r in imo.rends ) {
        var sr = ( SpriteRenderer )r;
        if ( ! sr.sprite || sr.sprite.texture != tex ) {
            UnityEngine.Object.Destroy( sr.sprite );
            sr.sprite = UnityEngine.Sprite.Create( tex, new Rect( 0.0f, 0.0f, tex.width, tex.height ),
                                                    Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect,
                                                    border != null ? border.Value : Vector4.zero );
        }
        sr.color = color != null ? color.Value : Color.white;
    }
    return imo.go;
}


}
