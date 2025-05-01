#version 330 core
// Use GLSL version 3.30 Core Profile

// Input vertex data (from VBO)
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor; // Keep color for potential future use, but don't pass it
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec3 aNormal;    // <<< Added normal attribute

// Uniforms for transformation matrices
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

// <<< Removed vColor output
out vec2 vTexCoord;
out vec3 FragPos;  // <<< Added: Vertex position in world space
out vec3 Normal;   // <<< Added: Normal vector in world space

void main()
{
    // Calculate world-space position of the vertex
    FragPos = vec3(uModel * vec4(aPosition, 1.0));
    
    // Calculate world-space normal
    // Use inverse transpose of model matrix for correct normal transformation
    // Normalization is important
    Normal = normalize(mat3(transpose(inverse(uModel))) * aNormal);

    // Transform vertex position for clipping and rasterization
    gl_Position = uProjection * uView * vec4(FragPos, 1.0); // Use FragPos here

    // <<< Removed vColor assignment
    vTexCoord = aTexCoord; // Pass tex coords through
} 