#version 330 core

in vec3 vColor; // Input color from vertex shader (interpolated)
in vec2 vTexCoord; // Added input for tex coords

// Output color for the fragment
out vec4 FragColor;

uniform sampler2D uTexture; // Added texture sampler uniform

void main()
{
    // Sample the texture
    vec4 texColor = texture(uTexture, vTexCoord);
    
    // For now, just use the texture color directly.
    // Could also mix with vertex color: FragColor = texColor * vec4(vColor, 1.0);
    FragColor = texColor;
} 