[System.Serializable]
public class PID {
	public float pFactor, iFactor, dFactor, outputLimit;
		
	float integral;
	float lastError;
	
	
	public PID(float pFactor, float iFactor, float dFactor, float outputLimit) {
		this.pFactor = pFactor;
		this.iFactor = iFactor;
		this.dFactor = dFactor;
        this.outputLimit = outputLimit;
	}
	
	
	public float Update(float setpoint, float actual, float timeFrame) {
		float present = setpoint - actual;
		integral += present * timeFrame;
		float deriv = (present - lastError) / timeFrame;
		lastError = present;

        float output = present * pFactor + integral * iFactor + deriv * dFactor;
        
        // If output is greater than limit, reset integrator 
        if (output > outputLimit) {
            integral = outputLimit - (present * pFactor + deriv * dFactor);
            return outputLimit;
        } else if (output < -1*outputLimit) {
            integral = (present * pFactor + deriv * dFactor) + outputLimit;
            return -1*outputLimit;
        }

		return output;
	}
}
