#version 330 core

in vec3 vColor; // Input color from vertex shader (interpolated)

// Output color for the fragment
out vec4 FragColor;

void main()
{
    // Use the interpolated vertex color
    FragColor = vec4(vColor, 1.0);
} 