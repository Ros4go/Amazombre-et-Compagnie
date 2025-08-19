Shader "Unlit/PlanarReflectionSimple"
{
    Properties
    {
        _ReflectionTex ("Reflection (RenderTexture)", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ReflectionTex;
            float4 _Tint;
            float _Intensity;

            // VP de la caméra de réflexion (passé par le script)
            float4x4 _ReflectionVP;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 projUV : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(UNITY_MATRIX_VP, worldPos);

                // coordonnées projetées par la caméra de réflexion
                float4 clip = mul(_ReflectionVP, worldPos);
                // passage clip  NDC  UV
                o.projUV = clip;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.projUV.xy / i.projUV.w;
                uv = uv * 0.5 + 0.5;        // NDC [-1,1] -> [0,1]

                // option : retournement vertical si besoin
                // uv.y = 1.0 - uv.y;

                fixed4 col = tex2D(_ReflectionTex, uv);
                col *= _Tint;
                col.rgb *= _Intensity;
                return col;
            }
            ENDCG
        }
    }
}
