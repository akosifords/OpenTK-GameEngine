#version 330 core
// Use GLSL version 3.30 Core Profile

// Input vertex data (from VBO)
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor; // Added color attribute
layout (location = 2) in vec2 aTexCoord;  // Added texture coordinates

// Uniforms for transformation matrices
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vColor; // Output color to fragment shader
out vec2 vTexCoord; // Added output for tex coords

void main()
{
    // Transform vertex position
    // Order: Projection * View * Model * VertexPosition
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    vColor = aColor; // Pass color through
    vTexCoord = aTexCoord; // Pass tex coords through
} 