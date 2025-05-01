#version 330 core
// Use GLSL version 3.30 Core Profile

// Input vertex data (from VBO)
layout (location = 0) in vec3 aPosition;

void main()
{
    // Output position directly (no transformation yet)
    gl_Position = vec4(aPosition, 1.0);
} 