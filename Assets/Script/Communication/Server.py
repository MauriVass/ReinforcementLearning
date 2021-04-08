import string
import cherrypy
import json
import numpy as np
import tensorflow as tf

class Network():
	def __init__(self,a):
		self.a = a
		self.actions = 4
		self.model = tf.keras.Sequential([
							tf.keras.layers.Flatten(),
							tf.keras.layers.Dense(8, activation='relu'),
                            tf.keras.layers.Dense(64, activation='relu'),
                            tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(self.actions)
						])

		self.model.compile(optimizer='adam',
              loss=['mae'],
              metrics=['mae'])
		#print(self.model.summary())


	def fit(self, input, target):
		input = tf.constant(input, shape=(1,self.actions+4))
		output = self.model.fit(input, target, epochs=1)
		return output[0]

	def predict(self, input):
		input = tf.constant(input, shape=(1,self.actions+4))
		output = self.model.predict(input)
		return output[0]


class CalculatorWebService(object):
	#Required to be accessable online
	exposed=True

	def __init__(self):
		self.net = Network('1.0')

	def FIT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		print(msg)
		b = self.net.fit()
		output = b
		return output

	def PREDICT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		input = np.array(arr.split('.')).astype(int)
		output = self.net.predict(input)
		out = '.'.join([str(o) for o in output])
		print(out, np.shape(out))
		return out

	def GET(self,*path,**query):
		output = 'Get'
		return output

	def POST(self,*path,**query):
		msg = cherrypy.request.body.read()
		print(msg)
		output = 'Post'
		return output

	def PUT(self,*path,**query):
		msg = cherrypy.request.body.read().decode("utf-8") 
		msg = msg.split('.')
		result = {'action':msg[4], 'value':[0.35,0]}
		output = json.dumps(result)
		return output

	def DELETE(self,*path,**query):
		output = 'Get'
		return output

#'request.dispatch': cherrypy.dispatch.MethodDispatcher() => switch from default URL to HTTP compliant approch
conf = { '/': {	'request.dispatch': cherrypy.dispatch.MethodDispatcher(),
								'tools.sessions.on':True} 
					}
cherrypy.tree.mount(CalculatorWebService(), '/', conf)

cherrypy.config.update({'servet.socket_host':'0.0.0.0'})
cherrypy.config.update({'servet.socket_port':'8080'})

cherrypy.engine.start()
cherrypy.engine.block()
