Shader "Hidden/Unlit/FOVFill"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,0.6)
        _Fill ("Fill", Range(0,1)) = 0
        _Radius ("Radius", Float) = 1
        _HalfAngle ("HalfAngle", Float) = 45
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Fill;
            float _Radius;
            float _HalfAngle;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 localPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // use X,Z as horizontal plane (object-space)
                float2 p = float2(i.localPos.x, i.localPos.z);
                float dist = length(p);
                if (dist > _Fill * _Radius + 1e-6) discard;

                float ang = degrees(atan2(p.x, p.y)); // x,z => angle where 0 is forward (+z)
                float half = abs(_HalfAngle);
                if (abs(ang) > half + 1e-6) discard;

                return _Color;
            }
            ENDCG
        }
    }
}
