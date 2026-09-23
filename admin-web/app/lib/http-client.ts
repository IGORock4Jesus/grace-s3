import type { AxiosError, AxiosRequestConfig } from "axios";
import Axios from "axios";

const instance = Axios.create({ baseURL: "" });

export const customInstance = async <T>(
	config: AxiosRequestConfig,
): Promise<T> => {
	const { data } = await instance({ ...config });
	return data;
};

export default customInstance;

export interface ErrorType<Error> extends AxiosError<Error> {}
