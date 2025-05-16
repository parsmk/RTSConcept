using System;
using System.Collections.Generic;
using UnityEngine;

public class BufferManager : IDisposable {
    public List<ComputeBuffer> buffers = new();

    private ComputeShader _cShader;
    private Material _material;
    private int _kernelID;

    public BufferManager(ComputeShader cShader, int kernelID) { 
        _cShader = cShader; 
        this._kernelID = kernelID;
    }
    public BufferManager(Material material, int kernelID) { 
        _material = material; 
        this._kernelID = kernelID;
    }

    public ComputeBuffer PrepareBuffer<T>(T[] data, int count, int stride, string bufferName) {
        ComputeBuffer output = new(count, stride);
        if (data != null) { output.SetData(data); }
        if (_cShader != null) _cShader.SetBuffer(_kernelID, bufferName, output);
        else _material.SetBuffer(_kernelID, output);
        buffers.Add(output);
        return output;
    }

    public ComputeBuffer PrepareConstantBuffer<T>(T data, int stride, string bufferName) {
        ComputeBuffer output = new(1, stride, ComputeBufferType.Constant);
        output.SetData(new[] { data });
        if (_cShader != null) _cShader.SetConstantBuffer(bufferName, output, 0, stride);
        else _material.SetConstantBuffer(bufferName, output, 0, stride);
        buffers.Add(output);
        return output;
    }

    public ComputeBuffer PrepareOutputBuffer(int count, int stride, string bufferName) {
        ComputeBuffer output = new(count, stride);
        if (_cShader != null) _cShader.SetBuffer(_kernelID, bufferName, output);
        else _material.SetBuffer(_kernelID, output);
        buffers.Add(output);
        return output;
    }

    public void Dispose() => buffers.ForEach(b => b.Release());

    ~BufferManager() { Dispose(); }
}