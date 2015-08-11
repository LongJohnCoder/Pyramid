
using namespace System::Collections::Generic;
using namespace Pyramid::Scrutinizer;

ref class AMDAsic_Impl;
ref class AMDShader_Impl;

ref class Scrutinizer_GCN : public Pyramid::Scrutinizer::IScrutinizer
{
public:

    Scrutinizer_GCN( AMDAsic_Impl^ asic, AMDShader_Impl^ shader )
        : m_pmAsic(asic), m_pmShader(shader)
    {
    }
    
    virtual unsigned int GetDefaultCUCount();

    virtual unsigned int GetDefaultOccupancy();
    
    virtual List<IInstruction^>^ BuildProgram( );

    virtual List<IInstruction^>^ BuildDXFetchShader( Pyramid::IDXShaderReflection^ refl );

    virtual System::String^ AnalyzeExecutionTrace( List<IInstruction^>^ ops, unsigned int nWaveIssueRate, unsigned int nOccupancy, unsigned int nCUCount );

private:
    AMDAsic_Impl^ m_pmAsic;
    AMDShader_Impl^ m_pmShader;
};
