Shader "Custom/OutlineShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)  // Color of the mesh
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)  // Outline color
        _Outline ("Outline width", Range (.002, 3)) = 1  // Thickness of the outline
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // Pass 1: Render the outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Always" }
            // Cull Front  // Render only the front faces for the outline

            ZWrite On
            ZTest LEqual
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Uniforms for the shader properties
            uniform float _Outline;
            uniform float4 _OutlineColor;

            // Vertex program
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                // Apply outline scaling
                v2f o;
                float3 norm = mul((float3x3) unity_ObjectToWorld, v.normal);
                v.vertex.xyz += norm * _Outline;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = _OutlineColor;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;  // Set the outline color
            }
            ENDCG
        }

        // Pass 2: Render the original mesh normally
        Pass
        {
            Name "Base"
            Tags { "LightMode" = "ForwardBase" }
            // Cull Back  // Render only the back faces (the regular mesh)

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;  // Set the mesh color
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
